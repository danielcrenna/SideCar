using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

			services.TryAddScoped<ArtifactService>();
			services.TryAddScoped<PackageService>();
			services.TryAddScoped<IArtifactResolver, WebArtifactResolver>();
			services.TryAddScoped<IPackageStore, PhysicalFilePackageStore>();
			services.TryAddScoped<IArtifactStore, PhysicalFileArtifactStore>();

			if (configureAction != null)
				services.Configure(configureAction);
			return services;
		}
	}
}
