using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Models;

namespace SideCar.Services
{
	public class PackageService
	{
		private readonly ArtifactService _artifacts;
		private readonly IPackageStore _store;
		private readonly IOptionsSnapshot<SideCarOptions> _options;
		private readonly ILogger<PackageService> _logger;

		public PackageService(ArtifactService artifacts, IPackageStore store, IOptionsSnapshot<SideCarOptions> options, ILogger<PackageService> logger)
		{
			_artifacts = artifacts;
			_store = store;
			_options = options;
			_logger = logger;
		}

		public async Task<PackageResult> PackageAsync(Assembly assembly, string buildHash, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var builds = await _artifacts.GetAvailableBuildsAsync(cancellationToken);
			if (!builds.Contains(buildHash))
				return null;

			var outputDir = Path.GetFullPath(_options.Value.PackagesLocation);
			var sdkDir = Path.GetFullPath(Path.Combine(_options.Value.BuildLocation, $"mono-wasm-{buildHash}"));
			var assemblyDir = Path.GetDirectoryName(assembly.Location);

			Directory.CreateDirectory(outputDir);
			if (!Directory.Exists(sdkDir))
			{
				Directory.CreateDirectory(sdkDir);
				ZipFile.ExtractToDirectory($"{sdkDir}.zip", sdkDir);
			}

			ComputePackageHash(assembly, buildHash);


			var sb = new StringBuilder();
			sb.Append($" --search-path=\"{assemblyDir}\"");			// Add specified path 'x' to list of paths used to resolve assemblies
			sb.Append($" --mono-sdkdir=\"{sdkDir}\"");				// Set the mono sdk directory to 'x'
			sb.Append($" --copy=always");							// Set the type of copy to perform (always|ifnewest)
			sb.Append($" --out=\"{outputDir}\"");					// Set the output directory to 'x' (default to the current directory)
			sb.Append($" {assembly.Location}");						// Include {target}.dll as one of the root assemblies
			var args = sb.ToString();                           

			var processPath = Path.Combine(sdkDir, "packager.exe");
			var process = new Process
			{
				StartInfo = new ProcessStartInfo(processPath, args)
				{
					UseShellExecute = false,
					WorkingDirectory = "",
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};

			var success = process.Start();
			var errors = await process.StandardError.ReadToEndAsync();
			var output = await process.StandardOutput.ReadToEndAsync();

			var result = new PackageResult
			{
				Successful = success,
				Errors = errors,
				Output = output
			};

			return result;
		}

		// FIXME: vary on content in the assembly!
		public string ComputePackageHash(ICustomAttributeProvider assembly, string buildHash)
		{
			var assemblyHash = (GuidAttribute) assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
			var packageHash = Encoding.UTF8.GetString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes($"{assemblyHash.Value}_{buildHash}")));
			return packageHash;
		}

		public Assembly FindPackageAssemblyByName(string package)
		{
			return _store.FindPackageAssemblyByName(package);
		}

		public Task<HashSet<string>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.PackagesLocation);

			var packages = Directory.EnumerateDirectories(_options.Value.BuildLocation);
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

		public Task<string> LoadPackageContentAsync(string packageHash, PackageFile runtimeJs, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
