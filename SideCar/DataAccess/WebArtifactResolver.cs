using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class WebArtifactResolver : IArtifactResolver
	{
		private readonly IOptionsSnapshot<SideCarOptions> _options;
		private readonly ILogger<WebArtifactResolver> _logger;

		public WebArtifactResolver(IOptionsSnapshot<SideCarOptions> options, ILogger<WebArtifactResolver> logger)
		{
			_options = options;
			_logger = logger;
		}

		public async Task<string> GetLatestStableBuildAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.BuildLocation);

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
				if (!a.HasAttribute("href"))
					continue;
				var href = a.Attributes["href"];
				if (!href.Value.Contains("github.com/mono/mono/commit/"))
					continue;
				var segments = new Uri(href.Value, UriKind.Absolute).Segments;
				buildHash = segments[segments.Length - 1].Substring(0, 11);
				break;
			}

			if (buildNumber == null || buildHash == null)
				return null;

			var artifactUrl = new Uri(string.Format(_options.Value.ArtifactMask, buildNumber, buildHash), UriKind.Absolute);
			var filePath = Path.Combine(_options.Value.BuildLocation, artifactUrl.Segments[artifactUrl.Segments.Length - 1]);
			if (File.Exists(filePath))
				return buildHash;

			await client.DownloadFileTaskAsync(artifactUrl, filePath);

			return !File.Exists(filePath) ? null : buildHash;
		}
	}
}