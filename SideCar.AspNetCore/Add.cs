// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SideCar.Configuration;

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
