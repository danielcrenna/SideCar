using Microsoft.AspNetCore.Blazor.Hosting;

namespace MyBlazorApp
{
    /// <summary>
    /// <remarks>
    /// This only exists to induce the Microsoft.NET.Sdk.Web to compile WebAssembly for this project.
    /// If the code is removed, then WebAssembly will not contain anything, and you'll get silent failures.
    /// </remarks>
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebAssemblyHostBuilder CreateHostBuilder(string[] args) =>
            BlazorWebAssemblyHost.CreateDefaultBuilder()
                .UseBlazorStartup<ClientStartup>();
    }
}
