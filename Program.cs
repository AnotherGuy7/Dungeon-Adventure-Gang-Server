namespace DAGServer
{
    public class Program
    {
        public static Server dagServer;

        public static void Main(string[] args)
        {
            dagServer = new Server();
            dagServer.CreateNewServer();
            while (true)
                dagServer.SearchForMessages();

        }
    }
}
