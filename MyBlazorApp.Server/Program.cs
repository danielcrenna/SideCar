using SideCar.Blazor.Server;

namespace MyBlazorApp.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SideCarServer.HybridApp<ClientStartup, ServerStartup>(args);
        }
    }
}
