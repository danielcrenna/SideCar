// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Configuration;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class PhysicalFilePackageStore : IPackageStore
	{
		private readonly IAssemblyResolver _assemblies;
		private readonly IOptionsSnapshot<SideCarOptions> _options;
		private readonly ILogger<PhysicalFileBuildStore> _logger;

		public PhysicalFilePackageStore(IAssemblyResolver assemblies, IOptionsSnapshot<SideCarOptions> options, ILogger<PhysicalFileBuildStore> logger)
		{
			_assemblies = assemblies;
			_options = options;
			_logger = logger;
		}

		public async Task<Assembly> FindAssemblyByNameAsync(string packageName, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var assemblies = await _assemblies.GetRegisteredAssembliesAsync();
			Assembly target = null;
			foreach (var assembly in assemblies)
			{
				var name = assembly.GetName().Name;
				if (!name.Equals(packageName, StringComparison.OrdinalIgnoreCase))
					continue;

				_logger?.LogDebug("Found package name {PackageName}", packageName);
				target = assembly;
				break;
			}

			if (target != null)
				return target;
			_logger?.LogWarning("No assembly found matching package name {PackageName}", packageName);
			return null;
		}

		public Task<HashSet<string>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.PackageLocation);

			var packages = Directory.EnumerateDirectories(_options.Value.PackageLocation);
			var ordered = packages.Select(x => new DirectoryInfo(x)).OrderByDescending(x => x.CreationTimeUtc);

			var directories = new HashSet<string>();
			foreach (var directory in ordered)
			{
				var match = Regex.Match(directory.Name, "mono-wasm-(\\w+)", RegexOptions.Compiled);
				if (match.Success)
					directories.Add(match.Groups[1].Value);
			}
			return Task.FromResult(directories);
		}

		public Task<byte[]> LoadPackageContentAsync(string packageHash, PackageFile packageFile, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.PackageLocation);

			var packagePath = Path.Combine(_options.Value.PackageLocation, $"mono-wasm-{packageHash}");
			if (!Directory.Exists(packagePath))
				return null;

			switch (packageFile)
			{
				case PackageFile.MonoConfig:
				{
					var fileName = Path.Combine(packagePath, "mono-config.js");
					return !File.Exists(fileName) ? null : Task.FromResult(File.ReadAllBytes(fileName));
				}
				case PackageFile.RuntimeJs:
				{
					var fileName = Path.Combine(packagePath, "runtime.js");
					return !File.Exists(fileName) ? null : Task.FromResult(File.ReadAllBytes(fileName));
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(packageFile), packageFile, null);
			}
		}

		public Task<byte[]> LoadManagedLibraryAsync(string packageHash, string filename, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.PackageLocation);

			var packagePath = Path.Combine(_options.Value.PackageLocation, $"mono-wasm-{packageHash}");
			if (!Directory.Exists(packagePath))
			{
				_logger?.LogDebug("Could not find managed library");
				return null;
			}

			var fileName = Path.Combine(packagePath, "managed", filename);
			return !File.Exists(fileName) ? null : Task.FromResult(File.ReadAllBytes(fileName));
		}
	}
}