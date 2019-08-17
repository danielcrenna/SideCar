using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SideCar
{
	public static class Add
	{
		public static IServiceCollection AddSideCar(this IServiceCollection services, IConfiguration config)
		{
			return services.AddSideCar(config.Bind);
		}

		public static IServiceCollection AddSideCar(this IServiceCollection services, Action<SideCarOptions> configureAction = null)
		{
			services.AddOptions();
			services.AddScoped<ArtifactService>();
			if (configureAction != null)
				services.Configure(configureAction);
			return services;
		}
	}
}
