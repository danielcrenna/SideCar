using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class PhysicalFileArtifactStore : IArtifactStore
	{
		private readonly IOptionsSnapshot<SideCarOptions> _options;
		private readonly ILogger<PhysicalFileArtifactStore> _logger;

		public PhysicalFileArtifactStore(IOptionsSnapshot<SideCarOptions> options, ILogger<PhysicalFileArtifactStore> logger)
		{
			_options = options;
			_logger = logger;
		}

		public Task<HashSet<string>> GetAvailableArtifactsAsync(CancellationToken cancellationToken)
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

		public async Task<string> LoadBuildContentAsync(string buildHash, ArtifactFile artifactFile, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.BuildLocation);

			var filePath = Path.Combine(_options.Value.BuildLocation, $"mono-wasm-{buildHash}.zip");
			if (!File.Exists(filePath))
				return null;

			using (var fs = File.OpenRead(filePath))
			{
				using (var zip = new ZipArchive(fs, ZipArchiveMode.Read, true))
				{
					foreach (var entry in zip.Entries)
					{
						switch (artifactFile)
						{
							case ArtifactFile.MonoJs:
								if (entry.FullName == "builds/release/mono.wasm")
									using (var sr = new StreamReader(entry.Open()))
										return await sr.ReadToEndAsync();
								break;
							case ArtifactFile.MonoWasm:
								if (entry.FullName == "builds/release/mono.wasm")
									using (var sr = new StreamReader(entry.Open()))
										return await sr.ReadToEndAsync();
								break;
							default:
								continue;
						}
					}
				}
			}

			return null;
		}
	}
}