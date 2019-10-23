using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MyBlazorApp
{
	public class ClientStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
	        /* Do not put anything in here, as it will likely result in a silently broken build */
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}