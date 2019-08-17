using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace SideCar.AspNetCore
{
    public class ArtifactController : Controller
    {
        private readonly ArtifactService _service;

        public ArtifactController(ArtifactService service)
        {
            _service = service;
        }

        [HttpOptions("artifacts")]
        public async Task<IActionResult> Options()
        {
            var versions = await _service.GetBuildsAsync(HttpContext.RequestAborted);
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

        private async Task<IActionResult> TryServeArtifactFileAsync(Artifact artifact, string version = null)
        {
            var cancel = HttpContext.RequestAborted;

            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag))
            {
                var resources = await _service.GetBuildsAsync(cancel);
                foreach (var hash in etag)
                    if (resources.Contains(hash))
                        return StatusCode((int)HttpStatusCode.NotModified);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                var resources = await _service.GetBuildsAsync(cancel);
                if (resources.Contains(version))
                    return await ServeArtifactFileAsync(artifact, version, cancel);
                return NotFound();
            }

            var buildHash = await _service.GetLatestStableBuildAsync(cancel);

            return await ServeArtifactFileAsync(artifact, buildHash, cancel);
        }

        private async Task<IActionResult> ServeArtifactFileAsync(Artifact artifact, string buildHash, CancellationToken cancel)
        {
            var file = await _service.GetArtifactAsync(buildHash, artifact, cancel);
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