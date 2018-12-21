using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace NetChangeV2 {
    class Connection {
        public int neighbour;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private TcpClient _tcpClient;
        public Routingtable routingtable;

        public Connection(int neighbour) {
            this.neighbour = neighbour;
            Setup();
        }

        public Connection(int n, TcpClient c, StreamReader r, StreamWriter w)
        {
            routingtable = new Routingtable();
            neighbour = n;
            _tcpClient = c;
            _streamReader = r;
            _streamWriter = w;
        }

        private void Setup() {
            routingtable = new Routingtable();

            var stream = (_tcpClient = new TcpClient("localhost", neighbour)).GetStream();
            _streamReader = new StreamReader(stream);
            _streamWriter = new StreamWriter(stream);
            _streamWriter.AutoFlush = true;
        }

        public void StartPolling() {
            new Thread(new ThreadStart(PollNotifications)).Start();
        }

        public void PollNotifications() {
            string line;
            try {
                while (true) {
                    line = _streamReader.ReadLine();

                    string[] message = line.Split(new char[] { ' ' }, 5);
                    switch (line[0]) {
                        // Receive routingTable - A targetPort port preferred distance    
                        case 'A':
                            Program.node.ReceiveChangeAlert(neighbour, new Route(int.Parse(message[2]), int.Parse(message[3]), message[4]));
                            break;
                        // Receive message      - B targetPort message
                        case 'B':
                            Program.node.SendMessage(line);
                            break;
                        // Disconnect           - D targetPort
                        case 'D':
                            //Program.node.ReceiveDisconnectMessage();
                            break;
                        default:
                            Console.WriteLine("Unknown command");
                            break;
                    }
                }
            } catch {
                Program.node.RemoveNeighbourConnection(neighbour);
                Console.WriteLine("hi3");
            }
        }

        internal void SendRoutingTable(Routingtable table) {
            lock (table)
            {
                foreach (Route r in table.Values)
                {
                    SendRoute(r);
                }
            }
        }

        public void CloseConnection() =>  _tcpClient.Close();
        private void Write(string message) => _streamWriter.WriteLine(message);
        public void SendMessage(string message) => Write(message);
        public void SendTCPHandShake() => Write("Hello");
        public void SendRoute(Route r) => Write("A " + neighbour + " " + r.MessageForm);
        public void SendOpeningMessage(int sender) => Write("X " + neighbour + " " + sender);
        public void SendDisconnectMessage(int sender) => Write("D " + sender);
    }
}