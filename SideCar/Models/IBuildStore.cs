// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IBuildStore
	{
		Task<HashSet<string>> GetAvailableBuildsAsync(CancellationToken cancellationToken);
		Task<byte[]> LoadBuildContentAsync(string buildHash, BuildFile buildFile, CancellationToken cancellationToken);
		Task<bool> TryProvisionBuildAsync(string buildHash, CancellationToken cancellationToken);
	}
}