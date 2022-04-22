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
            dagServer.CreateNewServer();
            while (!serverShutDown)
            {
                dagServer.SearchForMessages();
            }
            serverShutDown = false;
            Server.serverManager.Stop();
            start();
        }
    }
}
