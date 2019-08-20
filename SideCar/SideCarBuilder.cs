// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SideCar.Models;

namespace SideCar
{
	public class SideCarBuilder
	{
		public IAssemblyResolver Resolver { get; }
		public IServiceCollection Services { get; }

		public SideCarBuilder(IAssemblyResolver resolver, IServiceCollection services)
		{
			Resolver = resolver;
			Services = services;
		}
	}
}