using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SideCar
{
	public class PackageService
	{
		private readonly ArtifactService _artifacts;
		private readonly IOptionsSnapshot<SideCarOptions> _options;

		public PackageService(ArtifactService artifacts, IOptionsSnapshot<SideCarOptions> options)
		{
			_artifacts = artifacts;
			_options = options;
		}

		public async Task<bool> PackageAsync(Assembly assembly, string buildHash, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var builds = await _artifacts.GetBuildsAsync(cancellationToken);
			if (!builds.Contains(buildHash))
				return false;

			var outputDir = Path.GetFullPath(_options.Value.PackagesLocation);
			var sdkDir = Path.GetFullPath(Path.Combine(_options.Value.SdkLocation, $"mono-wasm-{buildHash}"));
			var assemblyDir = Path.GetDirectoryName(assembly.Location);

			Directory.CreateDirectory(outputDir);
			if (!Directory.Exists(sdkDir))
			{
				Directory.CreateDirectory(sdkDir);
				ZipFile.ExtractToDirectory($"{sdkDir}.zip", sdkDir);
			}
			
			var target = assembly.GetName().Name;

			var sb = new StringBuilder();
			sb.Append($" --search-path=\"{assemblyDir}\"");			// Add specified path 'x' to list of paths used to resolve assemblies
			sb.Append($" --mono-sdkdir=\"{sdkDir}\"");				// Set the mono sdk directory to 'x'
			sb.Append($" --copy=always");							// Set the type of copy to perform (always|ifnewest)
			sb.Append($" --out=\"{outputDir}\"");					// Set the output directory to 'x' (default to the current directory)
			sb.Append($" {assembly.Location}");						// Include {target}.dll as one of the root assemblies
			var args = sb.ToString();                           

			var processPath = Path.Combine(sdkDir, "packager.exe");
			var process = new Process();
			process.StartInfo = new ProcessStartInfo(processPath, args);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WorkingDirectory = "";
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			var result = process.Start();

			var errors = await process.StandardError.ReadToEndAsync();
			var output = await process.StandardOutput.ReadToEndAsync();

			return result;
		}
	}
}
