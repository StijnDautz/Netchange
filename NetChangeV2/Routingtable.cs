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
        public bool Removed { get { return preferred == "D"; } }
        public bool CanBe(int count) => distance > count || preferred == string.Empty;
        internal void Print() => Console.WriteLine(MessageForm);

        public static bool operator ==(Route r1, Route r2) {
            return r1.port == r2.port && 
                r1.distance == r2.distance && 
                r1.preferred == r2.preferred;
        }
        public static bool operator !=(Route r1, Route r2) {
            return r1.port != r2.port ||
                r1.distance != r2.distance ||
                r1.preferred != r2.preferred;
        }

        public Route(int port, int distance, string preferred) {
            this.port = port;
            this.distance = distance;
            this.preferred = preferred;
        }
    }

    class Routingtable : Dictionary<int, Route>
    {
        public void Add(Route info) {
            lock (this) Add(info.port, info);
        }

        /// <summary>
        /// This function is called when a port becomes unreachable after a port disconnected
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public void RemovePort(int port) {
            var info = this[port];
            lock(this) Remove(port);

            // Domjudge - Als het process op poort targetPort door een netwerkpartitie onbereikbaar is geworden:
            Console.WriteLine("Onbereikbaar: " + port);
        }

        internal bool removeOrAlter(Route route, int max) {
            var p = route.port;
            if (route.Removed) {
                return true;
            } else {
                this[p] = route;
                return false;
            }
        }

        internal bool TryAlter(Route r) {
            var p = r.port;
            if (r.Removed) lock (this) RemovePort(r.port);  //removed
            else if (!ContainsKey(p)) Add(r);               //added
            else if (this[p] != r) {
                this[p] = r;                                //changed
                Console.WriteLine("Afstand naar " + r.port + " is nu " + r.distance + " via " + r.preferred);            } else return false;                            //unchanged
            return true;
        }
    }
}