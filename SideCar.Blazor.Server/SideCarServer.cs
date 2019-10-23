// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace SideCar.Blazor.Server
{
	public static class SideCarServer
	{
		public static void HybridApp<TClientStartup, TServerStartup>(string[] args) where TServerStartup : class
		{
			var serverAppName = Assembly.GetCallingAssembly().GetName().Name;

			var builder = WebHost.CreateDefaultBuilder(args);

			builder.ConfigureServices(services =>
			{
				services.AddSingleton<SideCarService>();
			});

			builder.ConfigureAppConfiguration((context, _) =>
			{
				context.HostingEnvironment.ApplicationName = serverAppName;
			});

			builder.UseStaticWebAssets();
			
			var webHost = builder.UseStartup<SideCarStartup<TClientStartup, TServerStartup>>()
				.Build();

			webHost.Run();
		}
	}
}