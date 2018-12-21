using System;
using System.Threading;

namespace NetChangeV2
{
    class Program {
        public static Node node;
        public static int port;

        static void Main(string[] args) {
            //step 1 - create a server
            port = int.Parse(args[0]);

            new Server(port);
            node = new Node(port);

            //step 2 - connect with neighbours
            for (int i = 1; i < args.Length; i++) {
                while (true) {
                    try {
                        var neighbour = int.Parse(args[i]);

                        //protocol only initiate connection with greater neighbours
                        if (port < neighbour) break;
                        node.Connect(neighbour);

                        break;
                    }
                    catch { Thread.Sleep(100); }
                }
            }

            //step 3 - start interactive commandline
            new Thread(new ThreadStart(ConsoleCommander)).Start();
        }

        public static void ConsoleCommander()
        {
            Console.Title = "NetChange" + " " + port;

            string input;
            while (true)
            {
                input = Console.ReadLine();
                switch (input[0])
                {
                    case 'R': //Show routingtable
                        node.PrintRoutingTable();
                        break;
                    case 'B': //Send message
                        var tp = int.Parse(input.Split(new char[] { ' ' }, 3)[1]);
                        if (node.routingtable.ContainsKey(tp)) node.SendMessage(input);
                        else  Console.WriteLine("port " + tp + " is niet bekend");
                        break;
                    case 'C': //Create connection
                        node.Connect(int.Parse(input.Split(new char[] { ' ' }, 3)[1]));
                        break;
                    case 'D': //Disconnect
                        var np = int.Parse(input.Split(new char[] { ' ' }, 3)[1]);
                        if (node.routingtable.ContainsKey(np))
                            node.neighbours[np].CloseConnection();
                        else
                            Console.WriteLine("port " + np + " is niet bekend");
                        break;
                }
                Thread.Sleep(100);
            }
        }
    }
}