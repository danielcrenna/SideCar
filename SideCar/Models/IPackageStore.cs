// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IPackageStore
	{
		Task<Assembly> FindAssemblyByNameAsync(string packageName, CancellationToken cancellationToken);
		Task<HashSet<string>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default);
		Task<byte[]> LoadPackageContentAsync(string packageHash, PackageFile packageFile, CancellationToken cancellationToken);
		Task<byte[]> LoadManagedLibraryAsync(string packageHash, string fileName, CancellationToken cancellationToken);
	}
}