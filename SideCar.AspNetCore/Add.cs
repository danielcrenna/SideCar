using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SideCar.AspNetCore
{
	public static class Add
	{
		public static IServiceCollection AddSideCarApi(this IServiceCollection services, IConfiguration config)
		{
			return services.AddSideCarApi(config.Bind);
		}

		public static IServiceCollection AddSideCarApi(this IServiceCollection services,
			Action<SideCarOptions> configureAction = null)
		{
			services.AddSideCar(configureAction);
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
			return services;
		}
	}
}
