using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Microsoft.Extensions.Options;

namespace SideCar
{
    public class ArtifactService
    {
        private readonly IOptionsSnapshot<SideCarOptions> _options;
        
        public ArtifactService(IOptionsSnapshot<SideCarOptions> options)
        {
            _options = options;
        }

        public async Task<string> GetLatestStableBuildAsync(CancellationToken cancellationToken)
        {
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.SdkLocation);

			var client = new WebClient();
            var address = new Uri(_options.Value.ArtifactServer, UriKind.Absolute);
            var html = await client.DownloadStringTaskAsync(address);
            var parser = new AngleSharp.Html.Parser.HtmlParser();
            var document = await parser.ParseDocumentAsync(html, cancellationToken);
            var anchors = document.QuerySelectorAll("a");

            string buildNumber = null;
            var headers = document.QuerySelectorAll("h1");
            foreach (var h in headers)
            {
                var text = h.Text();
                var match = Regex.Match(text, "Build #([0-9]+)", RegexOptions.Compiled);
                if (match.Success)
                    buildNumber = match.Groups[1].Value; 
            }

            string buildHash = null;
            foreach (var a in anchors)
            {
                if (a.HasAttribute("href"))
                {
                    var href = a.Attributes["href"];
                    if (href.Value.Contains("github.com/mono/mono/commit/"))
                    {
                        var segments = new Uri(href.Value, UriKind.Absolute).Segments;
                        buildHash = segments[segments.Length - 1].Substring(0, 11);
                        break;
                    }
                }
            }

            if (buildNumber == null || buildHash == null)
                throw new InvalidOperationException();

            var artifactUrl = new Uri(string.Format(_options.Value.ArtifactMask, buildNumber, buildHash), UriKind.Absolute);
            var filePath = Path.Combine(_options.Value.SdkLocation, artifactUrl.Segments[artifactUrl.Segments.Length - 1]);
			if (File.Exists(filePath))
                return buildHash;

            await client.DownloadFileTaskAsync(artifactUrl, filePath);

            if (!File.Exists(filePath))
                throw new InvalidOperationException();

            return buildHash;
        }

        public Task<HashSet<string>> GetBuildsAsync(CancellationToken cancellationToken)
        {
			cancellationToken.ThrowIfCancellationRequested();
	        Directory.CreateDirectory(_options.Value.SdkLocation);

			var files = new HashSet<string>();
	        foreach (var file in Directory.EnumerateFiles(_options.Value.SdkLocation))
	        {
		        var match = Regex.Match(file, "mono-wasm-(\\W+)-.zip", RegexOptions.Compiled);
		        if (match.Success)
			        files.Add(match.Groups[1].Value);
	        }
	        return Task.FromResult(files);
        }

        public Task<Stream> GetArtifactAsync(string buildHash, Artifact artifact, CancellationToken cancellationToken)
        {
	        cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.SdkLocation);

			var filePath = Path.Combine(_options.Value.SdkLocation, $"mono-wasm-{buildHash}.zip");
			if (!File.Exists(filePath))
				return null;

			using (var fs = File.OpenRead(filePath))
			{
				using (var zip = new ZipArchive(fs, ZipArchiveMode.Read, true))
				{
					foreach (var entry in zip.Entries)
					{
						switch (artifact)
						{
							case Artifact.MonoJs:
								if (entry.FullName == "builds/release/mono.js")
									return Task.FromResult(entry.Open());
								break;
							case Artifact.MonoWasm:
								if (entry.FullName == "builds/release/mono.wasm")
									return Task.FromResult(entry.Open());
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(artifact), artifact, null);
						}
					}
				}
			}

			return null;
        }
    }
}
