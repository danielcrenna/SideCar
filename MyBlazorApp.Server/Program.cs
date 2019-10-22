using SideCar.Blazor.Server;

namespace MyBlazorApp.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SideCarServer.Bootstrap<ClientStartup, ServerStartup>(args);
        }
    }
}
