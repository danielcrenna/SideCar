using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Models;

namespace SideCar.Services
{
	public class ArtifactService
    {
	    private readonly IArtifactStore _store;
	    private readonly IArtifactResolver _resolver;
	    private readonly IOptionsSnapshot<SideCarOptions> _options;
	    private readonly ILogger<ArtifactService> _logger;

	    public ArtifactService(IArtifactStore store, IArtifactResolver resolver, IOptionsSnapshot<SideCarOptions> options, ILogger<ArtifactService> logger)
        {
	        _store = store;
	        _resolver = resolver;
	        _options = options;
	        _logger = logger;
        }

	    public async Task<HashSet<string>> GetAvailableBuildsAsync(CancellationToken cancellationToken)
	    {
		    cancellationToken.ThrowIfCancellationRequested();

		    return await _store.GetAvailableArtifactsAsync(cancellationToken);
	    }

		public async Task<string> GetLatestStableBuildAsync(CancellationToken cancellationToken)
        {
	        cancellationToken.ThrowIfCancellationRequested();

			if (!_options.Value.FetchArtifactsWhenMissing)
		        return (await _store.GetAvailableArtifactsAsync(cancellationToken)).FirstOrDefault();

			return await _resolver.GetLatestStableBuildAsync(cancellationToken);
        }

		public async Task<string> GetBuildByVersionAsync(string version = null, CancellationToken cancel = default)
		{
			string buildHash;
			if (string.IsNullOrWhiteSpace(version))
			{
				buildHash = await GetLatestStableBuildAsync(cancel);
			}
			else
			{
				var resources = await _store.GetAvailableArtifactsAsync(cancel);
				buildHash = resources.Contains(version) ? version : null;
			}

			return buildHash;
		}

        public async Task<string> LoadBuildContentAsync(string buildHash, ArtifactFile artifactFile, CancellationToken cancellationToken)
        {
	        return await _store.LoadBuildContentAsync(buildHash, artifactFile, cancellationToken);
        }
    }
}
