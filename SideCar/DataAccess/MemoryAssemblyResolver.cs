// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class MemoryAssemblyResolver : IAssemblyResolver
	{
		private readonly ConcurrentDictionary<string, Assembly> _assemblies;

		public MemoryAssemblyResolver()
		{
			_assemblies = new ConcurrentDictionary<string, Assembly>();
		}

		public Task<IEnumerable<Assembly>> GetRegisteredAssembliesAsync()
		{
			LazyLoadAppDomain();

			return Task.FromResult((IEnumerable<Assembly>) _assemblies.Values);
		}

		public void Register(Assembly assembly)
		{
			LazyLoadAppDomain();
			_assemblies.AddOrUpdate(assembly.GetName().Name, assembly, (k, v) => assembly);
			_assemblies.TryAdd(assembly.GetName().Name, assembly);
		}

		private void LazyLoadAppDomain()
		{
			if (_assemblies.Count != 0)
				return;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				_assemblies.TryAdd(assembly.GetName().Name, assembly);
		}
	}
}