using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Configuration;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class WebBuildResolver : IBuildResolver
	{
		private readonly IOptionsSnapshot<SideCarOptions> _options;
		private readonly ILogger<WebBuildResolver> _logger;

		public WebBuildResolver(IOptionsSnapshot<SideCarOptions> options, ILogger<WebBuildResolver> logger)
		{
			_options = options;
			_logger = logger;
		}

		public async Task<string> FetchLatestStableBuildAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Directory.CreateDirectory(_options.Value.BuildLocation);

			var client = new WebClient();
			var serverUrl = new Uri(_options.Value.ArtifactServer, UriKind.Absolute);

			_logger?.LogDebug("GET {ServerUrl}", serverUrl);
			var html = await client.DownloadStringTaskAsync(serverUrl);
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
				{
					buildNumber = match.Groups[1].Value;
					_logger?.LogDebug("Found build number {BuildNumber}", buildNumber);
				}
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
				_logger?.LogDebug("Found build hash {BuildHash}", buildHash);
				break;
			}

			if (buildNumber == null || buildHash == null)
				return null;

			var artifactUrl = new Uri(string.Format(_options.Value.ArtifactMask, buildNumber, buildHash), UriKind.Absolute);
			var filePath = Path.Combine(_options.Value.BuildLocation, artifactUrl.Segments[artifactUrl.Segments.Length - 1]);
			if (File.Exists(filePath))
			{
				_logger?.LogDebug("Build archive already fetched.");
				return buildHash;
			}

			_logger?.LogDebug("Downloading build archive from {ArtifactUrl} to {FilePath}", artifactUrl, filePath);
			await client.DownloadFileTaskAsync(artifactUrl, filePath);
			return !File.Exists(filePath) ? null : buildHash;
		}
	}
}