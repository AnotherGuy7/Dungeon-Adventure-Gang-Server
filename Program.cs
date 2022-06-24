using System;
using System.Timers;

namespace DAGServer
{
    public class Program
    {
        public static Server dagServer;
        public static bool serverShutDown = false;

        public static void Main(string[] args)
        {
            start();
        }

        //For automatic restarts
        public static void start()
        {
            dagServer = new Server();
            Server.clientData = new System.Collections.Generic.Dictionary<int, ServerData.ClientData>();
            Server.amountOfConnectedPlayers = 0;
            Server.dungeonEnemies = new int[255];
            Server.gameProjectileExists = new int[1000];
            Server.gameCurrentlyActive = false;
            Server.clientConnecting = false;
            dagServer.CreateNewServer();

            var timer = new Timer();
            timer.Interval = 1000 / 60;
            timer.Elapsed += (sender, e) =>
            {
                if(!serverShutDown)
                {
                    dagServer.SearchForMessages();
                }else
                {
                    timer.Dispose();
                    serverShutDown = false;
                    Server.serverManager.Stop();
                    Logger.UserFriendlyInfo("Restarting Server...");
                    start();
                }
            };

            timer.Start();

            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();


        }
    }
}
