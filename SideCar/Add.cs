using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SideCar.Configuration;
using SideCar.DataAccess;
using SideCar.Models;
using SideCar.Services;

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
			services.AddLogging();
			services.AddOptions();

			services.TryAddScoped<BuildService>();
			services.TryAddScoped<PackageService>();
			services.TryAddScoped<IBuildResolver, WebBuildResolver>();
			services.TryAddScoped<IBuildStore, PhysicalFileBuildStore>();
			services.TryAddScoped<IPackageCompiler, PhysicalFilePackageCompiler>();
			services.TryAddScoped<IPackageStore, PhysicalFilePackageStore>();
			
			if (configureAction != null)
				services.Configure(configureAction);
			return services;
		}
	}
}
