// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
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
		public static SideCarBuilder AddSideCar(this IServiceCollection services, IConfiguration config)
		{
			return services.AddSideCar(config.Bind);
		}

		public static SideCarBuilder AddSideCar(this IServiceCollection services, Action<SideCarOptions> configureAction = null)
		{
			var resolver = new MemoryAssemblyResolver();

			services.AddLogging();
			services.AddOptions();
			
			services.TryAddSingleton<IAssemblyResolver, MemoryAssemblyResolver>();

			services.TryAddScoped<BuildService>();
			services.TryAddScoped<PackageService>();
			services.TryAddScoped<ProxyService>();
			
			services.TryAddScoped<IBuildResolver, WebBuildResolver>();
			services.TryAddScoped<IBuildStore, PhysicalFileBuildStore>();
			services.TryAddScoped<IPackageCompiler, PhysicalFilePackageCompiler>();
			services.TryAddScoped<IPackageStore, PhysicalFilePackageStore>();
			
			if (configureAction != null)
				services.Configure(configureAction);

			return new SideCarBuilder(resolver, services);
		}

		public static SideCarBuilder AddPackageAssembly(this SideCarBuilder builder, Assembly assembly)
		{
			builder.Resolver.Register(assembly);
			return builder;
		}
	}
}
