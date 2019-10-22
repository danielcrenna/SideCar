// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SideCar.Blazor.Server
{
	public static class SideCarServer
	{
		public static void Bootstrap<TClientStartup, TServerStartup>(string[] args) where TServerStartup : class
		{
			var builder = WebHost.CreateDefaultBuilder(args);
			builder.UseStaticWebAssets();

			var appName = Assembly.GetCallingAssembly().GetName().Name;

			builder
				.ConfigureAppConfiguration((context, _) =>
				{
					context.HostingEnvironment.ApplicationName = appName;
				})
				.UseStartup<SideCarStartup<TClientStartup, TServerStartup>>()
				.Build()
				.Run();
		}
	}
}