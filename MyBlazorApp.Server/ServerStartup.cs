using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SideCar;
using SideCar.AspNetCore;

namespace MyBlazorApp.Server
{
    public class ServerStartup
    {
        private readonly IConfiguration _configuration;
        public ServerStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
	            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

			services.AddSideCarApi(o => { })
				.AddPackageAssembly(typeof(Simple.Complex).Assembly);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
