using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetChangeV2
{
    class Server {
        public Server(int poortnummer) {
            try {
                TcpListener listener = new TcpListener(IPAddress.Any, poortnummer);
                new Thread(() => PollNeighbourNotifications(listener)).Start();
            } catch (Exception e) { throw new Exception(e.ToString()); }
        }
        
        private void PollNeighbourNotifications(TcpListener listener) {
            listener.Start();
            while (true) {
                TcpClient client = listener.AcceptTcpClient();
                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());
                clientOut.AutoFlush = true;

                var message = clientIn.ReadLine();
                if (message.Split()[0] == "X") { //an unknown neighbour tries to connect
                    int neighbour = int.Parse(message.Split()[2]);
                    Program.node.AddNeighbour(neighbour, new Connection(neighbour, client, clientIn, clientOut));
                }
            }
        }
    }
}