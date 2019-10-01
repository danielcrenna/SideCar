// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ProxyService _proxies;
        private readonly ILogger<SideCarController> _logger;

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public SideCarController(BuildService builds, PackageService packages, ProxyService proxies, ILogger<SideCarController> logger)
        {
	        _builds = builds;
	        _packages = packages;
	        _proxies = proxies;
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
		
        [HttpGet("mono.js"), AllowAnonymous]
        public async Task<IActionResult> GetMonoJs([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeBuildFileAsync(BuildFile.MonoJs, version);
        }

        [HttpGet("mono.wasm"), AllowAnonymous]
        public async Task<IActionResult> GetMonoWasm([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeBuildFileAsync(BuildFile.MonoWasm, version);
        }

        [HttpGet("runtime.js"), AllowAnonymous]
        public async Task<IActionResult> GetRuntime([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
	        return await TryServePackageFileAsync(package, PackageFile.RuntimeJs, version);
		}

        [HttpGet("mono-config.js"), AllowAnonymous]
        public async Task<IActionResult> GetMonoConfig([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
			return await TryServePackageFileAsync(package, PackageFile.MonoConfig, version);
        }

        [HttpGet("managed/{fileName}"), AllowAnonymous]
        public async Task<IActionResult> GetManagedLibrary(string fileName)
        {
			// FIXME: duplicate code
			if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
	        {
		        var resources = await _packages.GetAvailablePackagesAsync(CancellationToken);
		        foreach (var hash in etag)
			        if (resources.Contains(hash))
				        return StatusCode((int) HttpStatusCode.NotModified);
	        }

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

        [HttpGet("sidecar.js"), AllowAnonymous]
        public async Task<IActionResult> GetSideCarJs([FromQuery(Name = "p")] string package = null, [FromQuery(Name = "v")] string version = null)
        {
	        return await TryServeProxyFile(package, ProxyFile.JavaScript, version);
        }

        [HttpGet("sidecar.ts"), AllowAnonymous]
        public async Task<IActionResult> GetTypeScriptProxy([FromQuery(Name = "p")] string package = null, [FromQuery(Name = "v")] string version = null)
        {
			return await TryServeProxyFile(package, ProxyFile.TypeScript, version);
		}
		
		#region Build Files

		private async Task<IActionResult> TryServeBuildFileAsync(BuildFile buildFile, string version = null)
        {
			// FIXME: duplicate code
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

	        // FIXME: duplicate code
			if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
			{
				var resources = await _packages.GetAvailablePackagesAsync(CancellationToken);
				foreach (var hash in etag)
					if (resources.Contains(hash))
						return StatusCode((int) HttpStatusCode.NotModified);
			}

			var (error, buildHash) = await TryGetBuildHash(version);
			if (error != null)
				return error;

			var assembly = await _packages.FindAssemblyByNameAsync(package, CancellationToken);
			if (assembly == null)
				return NotFound(new { Message = $"No package assemblies found matching name '{package}." });

			var packageHash = assembly.ComputePackageHash(buildHash);

			var packages = await _packages.GetAvailablePackagesAsync(CancellationToken);
			if (packages.Contains(packageHash))
			{
				var file = await ServePackageFileAsync(packageHash, packageFile, CancellationToken);
				if (!(file is NotFoundObjectResult))
					return file; // try to re-compile if we ran into an issue earlier
			}

			var result = await _packages.CompilePackageAsync(assembly, buildHash, CancellationToken);
			if (!result.Successful)
				return StatusCode((int) HttpStatusCode.InternalServerError, new { Message = "Package compile error." });

			return await ServePackageFileAsync(packageHash, packageFile, CancellationToken);
		}

		private async Task<IActionResult> ServePackageFileAsync(string packageHash, PackageFile packageFile,
	        CancellationToken cancel)
        {
	        var buffer = await _packages.LoadPackageContentAsync(packageHash, packageFile, cancel);
	        if (buffer == null || buffer.Length == 0)
	        {
		        return NotFound(new { Message = $"Package file {packageFile} for package {packageHash} not found."});
	        }

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

		#region Proxy Files

		private async Task<IActionResult> TryServeProxyFile(string package, ProxyFile proxyFile, string version)
		{
			if (string.IsNullOrWhiteSpace(package))
				return BadRequest(new { Message = "Package name required." });

			var (error, buildHash) = await TryGetBuildHash(version);
			if (error != null)
				return error;

			// FIXME: duplicate code
			if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
			{
				var resources = await _packages.GetAvailablePackagesAsync(CancellationToken);
				foreach (var hash in etag)
					if (resources.Contains(hash))
						return StatusCode((int) HttpStatusCode.NotModified);
			}

			var assembly = await _packages.FindAssemblyByNameAsync(package, CancellationToken);
			if (assembly == null)
				return NotFound(new { Message = $"No package assemblies found matching name '{package}." });

			return await ServeProxyFile(package, proxyFile, assembly, buildHash);
		}

		private async Task<IActionResult> ServeProxyFile(string package, ProxyFile proxyFile, Assembly assembly, string buildHash)
		{
			string proxy;
			switch (proxyFile)
			{
				case ProxyFile.JavaScript:
					proxy = await _proxies.GenerateJavaScriptProxy(package, CancellationToken);
					break;
				case ProxyFile.TypeScript:
					proxy = await _proxies.GenerateTypeScriptProxy(package, CancellationToken);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(proxyFile), proxyFile, null);
			}

			Response.Headers.Add(HeaderNames.ETag, assembly.ComputePackageHash(buildHash));
			Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");
			switch (proxyFile)
			{
				case ProxyFile.JavaScript:
					return File(Encoding.UTF8.GetBytes(proxy), "application/javascript");
				case ProxyFile.TypeScript:
					return File(Encoding.UTF8.GetBytes(proxy), "application/typescript");
				default:
					throw new ArgumentOutOfRangeException(nameof(proxyFile), proxyFile, null);
			}
		}

		#endregion

		private async Task<(IActionResult, string)> TryGetBuildHash(string version)
		{
			string buildHash = null;
			if (!string.IsNullOrWhiteSpace(version))
			{
				buildHash = await _builds.GetBuildByVersionAsync(version, CancellationToken);
				if (buildHash == null)
				{
					var result = NotFound(new { Message = $"Specified build {version} not found." });
					return (result, null);
				}
			}

			buildHash = buildHash ?? await _builds.GetLatestStableBuildAsync(CancellationToken);
			if (buildHash == null)
			{
				var result = NotFound(new {Message = "No builds found."});
				return (result, null);
			}

			return (null, buildHash);
		}
	}
}