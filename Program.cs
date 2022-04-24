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
            while (!serverShutDown)
            {
                dagServer.SearchForMessages();
            }
            serverShutDown = false;
            Server.serverManager.Stop();
            Logger.UserFriendlyInfo("\nRestarting Server...");
            start();
        }
    }
}
