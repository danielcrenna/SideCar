using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MyBlazorApp
{
	public class ClientStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
	        
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}