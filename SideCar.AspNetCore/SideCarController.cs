using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace SideCar.AspNetCore
{
	public class SideCarController : Controller
    {
        private readonly ArtifactService _artifacts;
        private readonly PackageService _packages;

        public SideCarController(ArtifactService artifacts, PackageService packages)
        {
	        _artifacts = artifacts;
	        _packages = packages;
        }

        [HttpOptions("artifacts")]
        public async Task<IActionResult> Options()
        {
            var versions = await _artifacts.GetBuildsAsync(HttpContext.RequestAborted);
            return Ok(new { data = versions });
        }

        [HttpGet("mono.js")]
        public async Task<IActionResult> GetMonoJs([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeArtifactFileAsync(Artifact.MonoJs, version);
        }

        [HttpGet("mono.wasm")]
        public async Task<IActionResult> GetMonoWasm([FromQuery(Name = "v")] string version = null)
        {
            return await TryServeArtifactFileAsync(Artifact.MonoWasm, version);
        }

        [HttpGet("runtime.js")]
        public async Task<IActionResult> GetPackage([FromQuery(Name = "p")] string package, [FromQuery(Name = "v")] string version = null)
        {
	        if (string.IsNullOrWhiteSpace(package))
				return BadRequest(new { Message = "Package name required." });

			var cancel = HttpContext.RequestAborted;

	        string buildHash;
			if (string.IsNullOrWhiteSpace(version))
			{
				buildHash = await _artifacts.GetLatestStableBuildAsync(cancel);
			}
			else
			{
				var resources = await _artifacts.GetBuildsAsync(cancel);
				if (resources.Contains(version))
					buildHash = version;
				else
					return NotFound();
			}

			if (buildHash == null)
				return NotFound(new { Message = "Build not found." });

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
	        foreach (var assembly in assemblies)
	        {
		        var name = assembly.GetName().Name;
		        if (!name.Equals(package, StringComparison.OrdinalIgnoreCase))
			        continue;

		        if (!await _packages.PackageAsync(assembly, buildHash, cancel))
				{
					return StatusCode((int) HttpStatusCode.InternalServerError, new
					{
						Message = "Build error."
					});
				}
		        else
		        {
			        return StatusCode((int) HttpStatusCode.NotImplemented);
		        }
	        }

	        return NotFound(new {Message = "Assembly not found."});
        }

		private async Task<IActionResult> TryServeArtifactFileAsync(Artifact artifact, string version = null)
        {
            var cancel = HttpContext.RequestAborted;

            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
            {
                var resources = await _artifacts.GetBuildsAsync(cancel);
                foreach (var hash in etag)
                    if (resources.Contains(hash))
                        return StatusCode((int)HttpStatusCode.NotModified);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                var resources = await _artifacts.GetBuildsAsync(cancel);
                if (resources.Contains(version))
                    return await ServeArtifactFileAsync(artifact, version, cancel);
                return NotFound();
            }

            var buildHash = await _artifacts.GetLatestStableBuildAsync(cancel);
            if (buildHash == null)
	            return NotFound();

            return await ServeArtifactFileAsync(artifact, buildHash, cancel);
        }

        private async Task<IActionResult> ServeArtifactFileAsync(Artifact artifact, string buildHash, CancellationToken cancel)
        {
            var file = await _artifacts.GetArtifactAsync(buildHash, artifact, cancel);
            if (file == null)
                return NotFound();
            Response.Headers.Add(HeaderNames.ETag, buildHash);
            Response.Headers.Add(HeaderNames.CacheControl, "public,max-age=31536000");
            switch (artifact)
            {
                case Artifact.MonoJs:
                    return File(Encoding.UTF8.GetBytes(file), "text/javascript");
                case Artifact.MonoWasm:
                    return File(Encoding.UTF8.GetBytes(file), "application/wasm");
                default:
                    throw new ArgumentOutOfRangeException(nameof(artifact), artifact, null);
            }
        }
    }
}