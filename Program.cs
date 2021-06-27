using Lidgren.Network;
using System;

namespace DAGServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Server.CreateNewServer();
            while (true)
                Server.SearchForMessages(Server.mainServer);

        }
    }
}
