using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SideCar.Services;

namespace SideCar.AspNetCore
{
	public class SideCarController : Controller
    {
        private readonly ArtifactService _artifacts;
        private readonly PackageService _packages;

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public SideCarController(ArtifactService artifacts, PackageService packages)
        {
	        _artifacts = artifacts;
	        _packages = packages;
        }

        [HttpOptions("builds")]
        public async Task<IActionResult> BuildOptions()
        {
            var versions = await _artifacts.GetAvailableBuildsAsync(CancellationToken);
            return Ok(new { data = versions });
        }

        [HttpOptions("packages")]
        public async Task<IActionResult> PackageOptions()
        {
			// FIXME: provide assembly and build info!
	        var versions = await _packages.GetAvailablePackagesAsync(CancellationToken);
	        return Ok(new { data = versions });
        }

		[HttpGet("mono.js")]
        public async Task<IActionResult> GetMonoJs([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeArtifactFileAsync(ArtifactFile.MonoJs, version);
        }

        [HttpGet("mono.wasm")]
        public async Task<IActionResult> GetMonoWasm([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeArtifactFileAsync(ArtifactFile.MonoWasm, version);
        }

        [HttpGet("runtime.js")]
        public async Task<IActionResult> GetRuntime([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
	        return await TryServePackageFileAsync(package, PackageFile.RuntimeJs, version);
		}

        [HttpGet("mono-config.js")]
        public async Task<IActionResult> GetMonoConfig([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
			return await TryServePackageFileAsync(package, PackageFile.RuntimeJs, version);
        }

		private async Task<IActionResult> TryServeArtifactFileAsync(ArtifactFile artifactFile, string version = null)
        {
            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
            {
                var resources = await _artifacts.GetAvailableBuildsAsync(CancellationToken);
                foreach (var hash in etag)
                    if (resources.Contains(hash))
                        return StatusCode((int)HttpStatusCode.NotModified);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
	            var versionHash = await _artifacts.GetBuildByVersionAsync(version, CancellationToken);
	            if (versionHash == null)
		            return NotFound(new { Message = $"Specified build version '{version}' not found." });

	            return await ServeArtifactFileAsync(version, artifactFile, CancellationToken);
			}

            var buildHash = await _artifacts.GetLatestStableBuildAsync(CancellationToken);
            if (buildHash == null)
	            return NotFound(new { Message = "No builds found."});

            return await ServeArtifactFileAsync(buildHash, artifactFile, CancellationToken);
        }

        private async Task<IActionResult> ServeArtifactFileAsync(string buildHash, ArtifactFile artifactFile,
	        CancellationToken cancel)
        {
            var file = await _artifacts.LoadBuildContentAsync(buildHash, artifactFile, cancel);
            if (file == null)
                return NotFound();
            Response.Headers.Add(HeaderNames.ETag, buildHash);
            Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");
            switch (artifactFile)
            {
                case ArtifactFile.MonoJs:
                    return File(Encoding.UTF8.GetBytes(file), "text/javascript");
                case ArtifactFile.MonoWasm:
                    return File(Encoding.UTF8.GetBytes(file), "application/wasm");
                default:
                    throw new ArgumentOutOfRangeException(nameof(artifactFile), artifactFile, null);
            }
        }

        private async Task<IActionResult> TryServePackageFileAsync(string package, PackageFile packageFile, string version = null)
        {
	        if (string.IsNullOrWhiteSpace(package))
		        return BadRequest(new { Message = "Package name required." });

			if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
			{
				var resources = await _packages.GetAvailablePackagesAsync(CancellationToken);
				foreach (var hash in etag)
					if (resources.Contains(hash))
						return StatusCode((int) HttpStatusCode.NotModified);
			}

			string buildHash;
			if (!string.IsNullOrWhiteSpace(version))
			{
				buildHash = await _artifacts.GetBuildByVersionAsync(version, CancellationToken);
				if (buildHash == null)
					return NotFound(new { Message = $"Specified build version '{version}' not found." });
			}

			buildHash = await _artifacts.GetLatestStableBuildAsync(CancellationToken);
			if (buildHash == null)
				return NotFound(new { Message = "No builds found." });

			var assembly = _packages.FindPackageAssemblyByName(package);
			if (assembly == null)
				return NotFound(new { Message = $"No package assemblies found matching name '{package}." });

			var result = await _packages.PackageAsync(assembly, buildHash, CancellationToken);
			if (!result.Successful)
				return StatusCode((int) HttpStatusCode.InternalServerError, new { Message = "Build error." });

			var packageHash = _packages.ComputePackageHash(assembly, buildHash);
			return await ServePackageFileAsync(packageHash, packageFile, CancellationToken);
		}

		private async Task<IActionResult> ServePackageFileAsync(string packageHash, PackageFile packageFile,
	        CancellationToken cancel)
        {
	        var file = await _packages.LoadPackageContentAsync(packageHash, packageFile, cancel);
	        if (file == null)
		        return NotFound();
	        Response.Headers.Add(HeaderNames.ETag, packageHash);
	        Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");
	        switch (packageFile)
	        {
		        case PackageFile.MonoConfig:
			        return File(Encoding.UTF8.GetBytes(file), "text/javascript");
		        case PackageFile.RuntimeJs:
			        return File(Encoding.UTF8.GetBytes(file), "text/javascript");
		        default:
			        throw new ArgumentOutOfRangeException(nameof(packageFile), packageFile, null);
	        }
        }
	}
}