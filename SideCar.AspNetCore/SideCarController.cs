// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SideCar.Extensions;
using SideCar.Models;
using SideCar.Services;

namespace SideCar.AspNetCore
{
	public class SideCarController : Controller
    {
        private readonly BuildService _builds;
        private readonly PackageService _packages;
        private readonly ILogger<SideCarController> _logger;

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public SideCarController(BuildService builds, PackageService packages, ILogger<SideCarController> logger)
        {
	        _builds = builds;
	        _packages = packages;
	        _logger = logger;
        }

        [HttpOptions("builds")]
        public async Task<IActionResult> BuildOptions()
        {
            var versions = await _builds.GetAvailableBuildsAsync(CancellationToken);
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
            return await TryServeBuildFileAsync(BuildFile.MonoJs, version);
        }

        [HttpGet("mono.wasm")]
        public async Task<IActionResult> GetMonoWasm([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeBuildFileAsync(BuildFile.MonoWasm, version);
        }

        [HttpGet("runtime.js")]
        public async Task<IActionResult> GetRuntime([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
	        return await TryServePackageFileAsync(package, PackageFile.RuntimeJs, version);
		}

        [HttpGet("mono-config.js")]
        public async Task<IActionResult> GetMonoConfig([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
			return await TryServePackageFileAsync(package, PackageFile.MonoConfig, version);
        }

        [HttpGet("managed/{fileName}")]
        public async Task<IActionResult> GetManagedLibrary(string fileName)
        {
			// FIXME: modify configuration to pass-through the package ID in the path
			var packageHash = (await _packages.GetAvailablePackagesAsync(CancellationToken)).LastOrDefault();
			if (packageHash == null)
				return NotFound(new {Message = "No package found"});

			var buffer = await _packages.LoadManagedLibraryAsync(packageHash, fileName, CancellationToken);
			if (buffer == null)
				return NotFound(new { Message = $"Package {packageHash} not found." });

			Response.Headers.Add(HeaderNames.ETag, packageHash);
			Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");

			return File(buffer, "application/octet-stream");
		}

		#region Build Files

		private async Task<IActionResult> TryServeBuildFileAsync(BuildFile buildFile, string version = null)
        {
            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
            {
                var resources = await _builds.GetAvailableBuildsAsync(CancellationToken);
                foreach (var hash in etag)
                    if (resources.Contains(hash))
                        return StatusCode((int)HttpStatusCode.NotModified);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
	            var versionHash = await _builds.GetBuildByVersionAsync(version, CancellationToken);
	            if (versionHash == null)
		            return NotFound(new { Message = $"Specified build {version} not found." });

	            return await ServeBuildFileAsync(version, buildFile, CancellationToken);
			}
			
            var buildHash = await _builds.GetLatestStableBuildAsync(CancellationToken);
            if (buildHash == null)
	            return NotFound(new { Message = "No builds found."});

            return await ServeBuildFileAsync(buildHash, buildFile, CancellationToken);
        }

        private async Task<IActionResult> ServeBuildFileAsync(string buildHash, BuildFile buildFile,
	        CancellationToken cancel)
        {
            var buffer = await _builds.LoadBuildContentAsync(buildHash, buildFile, cancel);
            if (buffer == null)
                return NotFound();
            Response.Headers.Add(HeaderNames.ETag, buildHash);
            Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");
            switch (buildFile)
            {
                case BuildFile.MonoJs:
                    return File(buffer, "application/javascript");
                case BuildFile.MonoWasm:
                    return File(buffer, "application/wasm");
                default:
                    throw new ArgumentOutOfRangeException(nameof(buildFile), buildFile, null);
            }
        }

		#endregion

		#region Package Files

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

			string buildHash = null;
			if (!string.IsNullOrWhiteSpace(version))
			{
				buildHash = await _builds.GetBuildByVersionAsync(version, CancellationToken);
				if (buildHash == null)
					return NotFound(new { Message = $"Specified build {version} not found." });
			}
			buildHash = buildHash ?? await _builds.GetLatestStableBuildAsync(CancellationToken);
			if (buildHash == null)
				return NotFound(new { Message = "No builds found." });

			var assembly = await _packages.FindPackageAssemblyByNameAsync(package, CancellationToken);
			if (assembly == null)
				return NotFound(new { Message = $"No package assemblies found matching name '{package}." });

			var packageHash = assembly.ComputePackageHash(buildHash);

			var packages = await _packages.GetAvailablePackagesAsync(CancellationToken);
			if (packages.Contains(packageHash))
				return await ServePackageFileAsync(packageHash, packageFile, CancellationToken);

			var result = await _packages.CompilePackageAsync(assembly, buildHash, CancellationToken);
			if (!result.Successful)
				return StatusCode((int) HttpStatusCode.InternalServerError, new { Message = "Package compile error." });

			return await ServePackageFileAsync(packageHash, packageFile, CancellationToken);
		}

		private async Task<IActionResult> ServePackageFileAsync(string packageHash, PackageFile packageFile,
	        CancellationToken cancel)
        {
	        var buffer = await _packages.LoadPackageContentAsync(packageHash, packageFile, cancel);
	        if (buffer == null)
		        return NotFound(new { Message = $"Package {packageHash} not found."});

	        Response.Headers.Add(HeaderNames.ETag, packageHash);
	        Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");
	        switch (packageFile)
	        {
		        case PackageFile.MonoConfig:
			        return File(buffer, "application/javascript");
		        case PackageFile.RuntimeJs:
			        return File(buffer, "application/javascript");
		        default:
			        throw new ArgumentOutOfRangeException(nameof(packageFile), packageFile, null);
	        }
        }

		#endregion
	}
}