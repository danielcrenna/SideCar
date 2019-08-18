// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Configuration;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class PhysicalFileBuildStore : IBuildStore
	{
		private readonly IOptionsSnapshot<SideCarOptions> _options;
		private readonly ILogger<PhysicalFileBuildStore> _logger;

		public PhysicalFileBuildStore(IOptionsSnapshot<SideCarOptions> options, ILogger<PhysicalFileBuildStore> logger)
		{
			_options = options;
			_logger = logger;
		}

		public Task<HashSet<string>> GetAvailableBuildsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.BuildLocation);

			var files = new HashSet<string>();
			var artifacts = Directory.EnumerateFiles(_options.Value.BuildLocation);
			var ordered = artifacts.Select(x => new FileInfo(x)).OrderByDescending(x => x.CreationTimeUtc);

			foreach (var file in ordered)
			{
				var match = Regex.Match(file.Name, "mono-wasm-(\\w+).zip", RegexOptions.Compiled);
				if (match.Success)
					files.Add(match.Groups[1].Value);
			}

			return Task.FromResult(files);
		}

		public async Task<byte[]> LoadBuildContentAsync(string buildHash, BuildFile buildFile, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.BuildLocation);

			var filePath = Path.Combine(_options.Value.BuildLocation, $"mono-wasm-{buildHash}.zip");
			if (!File.Exists(filePath))
			{
				_logger?.LogWarning("{FilePath} not found", filePath);
				return null;
			}

			var ms = new MemoryStream();

			using (var fs = File.OpenRead(filePath))
			{
				using (var zip = new ZipArchive(fs, ZipArchiveMode.Read, true))
				{
					foreach (var entry in zip.Entries)
					{
						switch (buildFile)
						{
							case BuildFile.MonoJs:
								if (entry.FullName == "builds/release/mono.js")
								{
									var stream = entry.Open();
									await stream.CopyToAsync(ms);
									return ms.ToArray();
								}
								break;
							case BuildFile.MonoWasm:
								if (entry.FullName == "builds/release/mono.wasm")
								{
									var stream = entry.Open();
									await stream.CopyToAsync(ms);
									return ms.ToArray();
								}
								break;
							default:
								continue;
						}
					}
				}
			}

			return null;
		}

		public Task<bool> TryProvisionBuildAsync(string buildHash, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var sdkDir = Path.GetFullPath(Path.Combine(_options.Value.BuildLocation, $"mono-wasm-{buildHash}"));
				if (Directory.Exists(sdkDir))
				{
					_logger?.LogDebug("Build {BuildHash} already provisioned", buildHash);
					return Task.FromResult(true);
				}

				_logger?.LogDebug("Provisioning build {BuildHash}", buildHash);
				Directory.CreateDirectory(sdkDir);
				ZipFile.ExtractToDirectory($"{sdkDir}.zip", sdkDir);
				_logger?.LogDebug("SDK extracted to {SdkDir}", sdkDir);

				return Task.FromResult(true);
			}
			catch (Exception e)
			{
				_logger?.LogError(e, "Failed to provided {BuildHash}", buildHash);
				return Task.FromResult(false);
			}
		}
	}
}