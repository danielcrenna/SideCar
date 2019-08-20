// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IAssemblyResolver
	{
		Task<IEnumerable<Assembly>> GetRegisteredAssembliesAsync();
		void Register(Assembly assembly);
	}
}