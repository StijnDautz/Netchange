using System;
using System.Collections.Generic;
using System.Linq;

namespace NetChangeV2 {
    // Deze klasse is in principe een extensie van de Program klasse. Dit is de enige plek waar dit object gebruikt wordt.
    class Node {
        public int port;
        public Dictionary<int, Connection> neighbours;
        public Routingtable routingtable;

        public Node(int p) {
            port = p;
            neighbours = new Dictionary<int, Connection>();
            routingtable = new Routingtable();
            lock (routingtable) routingtable.Add(new Route(port, 0, "local"));
        }

        /// <summary> Handles a change alert and sends the message to its neighbours if something changed in its routingtable </summary>
        internal void ReceiveChangeAlert(int np, Route r) {
            var neighbourtable = neighbours[np].routingtable;

            if (r.Removed || (!r.Removed && r.distance > routingtable.Count-1)) {
                Console.WriteLine("note to remove " + r.port + " from " + np);
                neighbourtable.RemovePort(r.port);
                if (!routingtable.ContainsKey(r.port)) return; // we already know
            } else neighbourtable.TryAlter(r);

            // check if the updated routingtable affects the own routingtable
            if (r.port != port) {
                var route = GetFastestRoute(r.port);
                if (routingtable.TryAlter(route)) AlertChange(route);
            }
        }

        private void AlertChange(Route r) {
            lock (neighbours) foreach (Connection c in neighbours.Values) c.SendRoute(r);
        }

        /* ---------------------------------------------- ACTIONS AFTER WRITE ----------------------------------------------*/
        public void PrintRoutingTable() {
            lock (routingtable) foreach (Route r in routingtable.Values) r.Print();
        }

        public void SendMessage(string input) {
            var message = input.Split(new char[] { ' ' }, 3);
            int tp = int.Parse(message[1]);

            if (tp != port) {
                var next = int.Parse(routingtable[tp].preferred);
                neighbours[next].SendMessage(input);
                // Domjudge - Als een bericht wordt doorgestuurd:
                Console.WriteLine("Bericht voor " + tp + " doorgestuurd naar " + next);
            }
            else Console.WriteLine(message[2]);
        }

        internal void ReceiveDisconnectMessage(int port) {
            //now that the message has arrived, we can safely close the connection
            neighbours[port].CloseConnection();

            var route = neighbours[port].routingtable[port];
            route.preferred = "D";

            //notify existing neighbours about route change
            lock(neighbours) neighbours.Remove(port);
            RecomputeBrokenPorts(port.ToString());

            route = GetFastestRoute(port);
            if (route.Removed || (!route.Removed && route.distance > routingtable.Count)) {
                routingtable.RemovePort(port);
            } else routingtable[port] = route;
            AlertChange(route);
        }

        public void Connect(int p) {
            var connection = new Connection(p);
            connection.SendOpeningMessage(port);
            AddNeighbour(p, connection);
        }

        public void Disconnect(int np) {
            var c = neighbours[np];
            c.SendDisconnectMessage(port);
            RemoveNeighbourConnection(np);
        }

        /* ---------------------------------------------- ACTIONS AFTER READ ----------------------------------------------*/
        public void AddNeighbour(int neighbour, Connection connection) {
            //lock, as we do not want to alter it when its being used
            lock (neighbours) neighbours.Add(neighbour, connection);

            var newroute = new Route(neighbour, 1, neighbour.ToString());
            routingtable.TryAlter(newroute);
            AlertChange(newroute);

            connection.SendRoutingTable(routingtable);
            connection.StartPolling();

            // Domjudge - Bij het ontstaan van een directe verbinding met het process op poort targetPort:
            Console.WriteLine("Verbonden: " + connection.neighbour);
        }

        /// <summary>Removes the neighbour from the neighbour collection and the routingtable</summary>
        public void RemoveNeighbourConnection(int np) {
            // Domjudge - Bij het verbreken van een directe verbinding met het process op poort neighbourPort:
            Console.WriteLine("Verbroken: " + np);

            lock(neighbours) neighbours.Remove(np);
            RecomputeBrokenPorts(np.ToString());
        }

        /// <summary>Recomputes the fastest route to ports, which route was broken after the preferred neighbour disconnected</summary>
        private void RecomputeBrokenPorts(string np) {
            Console.WriteLine("Recomputing broken ports");
            lock (routingtable) {
                //alert alternative routes
                foreach (Route i in routingtable.Values.Reverse()) {
                    if (i.preferred == np) {
                        // we are sure that every route to i.port that preferred np, is either unreachable or changed
                        var route = GetFastestRoute(i.port);
                        if (routingtable.removeOrAlter(route, routingtable.Count)) {
                            routingtable.RemovePort(i.port);
                            AlertChange(new Route(i.port, 1000, "D"));
                        } else AlertChange(route);
                    }
                }
            }
        }

        /* ---------------------------------------------- ROUTINGTABLE ----------------------------------------------*/
        private Route GetFastestRoute(int tp) {
            int d; int closest = int.MaxValue; Connection pn = null;

            lock (neighbours) {
                foreach (Connection c in neighbours.Values) {
                    if (tp == c.neighbour) return new Route(tp, 1, tp.ToString());
                    else if (c.routingtable.ContainsKey(tp)) {
                        d = c.routingtable[tp].distance;
                        if (closest > d && int.Parse(c.routingtable[tp].preferred) != port) { //cannot target a port through ourselves
                            closest = d;
                            pn = c;
                        }
                    }
                }
            }
            // if closest is bigger then the total routingtable, then the port has become unreachable
            return pn == null ? new Route(tp, 1000, "D") : new Route(tp, ++closest, Convert.ToString(pn.neighbour));
        }
    }
}