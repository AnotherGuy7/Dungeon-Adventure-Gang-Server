namespace DAGServer
{
    public class Program
    {
        public static Server dagServer;
        public static bool serverShutDown = false;

        public static void Main(string[] args)
        {
            dagServer = new Server();
            dagServer.CreateNewServer();
            while (!serverShutDown)
                dagServer.SearchForMessages();

        }
    }
}
