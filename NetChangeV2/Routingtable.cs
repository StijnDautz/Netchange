using System;
using System.Collections.Generic;

namespace NetChangeV2
{
    // Deze klasse representeerd de data voor elke node in de routingtabel
    class Route
    {
        public int port, distance;
        // the preferred node is a neighbour
        public string preferred;
        public string MessageForm { get { return port + " " + distance + " " + preferred; } }

        public bool CanBe(int count) {
            return distance > count || preferred == string.Empty;
        }

        public static bool operator ==(Route r1, Route r2)
        {
            return r1.port == r2.port && 
                r1.distance == r2.distance && 
                r1.preferred == r2.preferred;
        }
        public static bool operator !=(Route r1, Route r2)
        {
            return r1.port != r2.port ||
                r1.distance != r2.distance ||
                r1.preferred != r2.preferred;
        }

        public Route(int port, int distance, string preferred)
        {
            this.port = port;
            this.distance = distance;
            this.preferred = preferred;
        }

        internal void Update(int newDistance, string newPreferred)
        {
            distance = newDistance;
            preferred = newPreferred;
        }

        internal Route CompareAndChange(int newDistance, string newPreferred)
        {
            // if the info changes, return changed info else null
            var info = distance != newDistance || preferred != newPreferred ? this : null;

            distance = newDistance;
            preferred = newPreferred;

            return info;
        }
    }

    class Routingtable : Dictionary<int, Route>
    {
        public void Add(int targetPort, int distance, string preferred)
        {
            lock (this)
            {
                Add(targetPort, new Route(targetPort, distance, preferred));
            }
        }

        public void Add(Route info)
        {
            lock (this)
            {
                Add(info.port, info);
            }
        }

        public Route CompareAndChange(int port, int distance, Connection neighbour)
        {
            lock (this)
            {                
                if (!ContainsKey(port))
                {
                    Add(port, distance, neighbour.neighbour.ToString());
                    return this[port];
                }
                else
                    return this[port].CompareAndChange(distance, neighbour.neighbour.ToString());
            }
        }

        /// <summary>
        /// This function is called when a port becomes unreachable after a port disconnected
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        internal Route HandleUnreachablePort(int port)
        {
            var info = this[port];
            Remove(port);// TODO does not work, since we remove while we are iterating over this collection

            // Domjudge - Als het process op poort targetPort door een netwerkpartitie onbereikbaar is geworden:
            Console.WriteLine("Onbereikbaar: " + port);

            return info;
        }

        internal void removeOrAlter(Route route) {
            var p = route.port;
            if (route.CanBe(Count)) {
                HandleUnreachablePort(p);
            } else {
                this[p] = route;
            }
        }

        internal bool GetAndTryChange(Route route) {
            var p = route.port;
            //if (route.CanBe(Count)) {
            //    HandleUnreachablePort(p);
            //}
            if (!ContainsKey(p)) {
                Add(route);
            } else if (this[p] != route) {
                this[p] = route;
            } else {
                return false;
            }
            return true;
        }
    }
}