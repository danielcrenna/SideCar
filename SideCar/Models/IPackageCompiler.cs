﻿// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IPackageCompiler
	{
		Task<PackageResult> CompilePackageAsync(Assembly assembly, string buildHash, CancellationToken cancellationToken);
	}
}