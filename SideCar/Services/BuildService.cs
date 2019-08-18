using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Configuration;
using SideCar.Models;

namespace SideCar.Services
{
	public class BuildService
    {
	    private readonly IBuildStore _store;
	    private readonly IBuildResolver _resolver;
	    private readonly IOptionsSnapshot<SideCarOptions> _options;
	    private readonly ILogger<BuildService> _logger;

	    public BuildService(IBuildStore store, IBuildResolver resolver, IOptionsSnapshot<SideCarOptions> options, ILogger<BuildService> logger)
        {
	        _store = store;
	        _resolver = resolver;
	        _options = options;
	        _logger = logger;
        }

	    public async Task<HashSet<string>> GetAvailableBuildsAsync(CancellationToken cancellationToken = default)
	    {
		    cancellationToken.ThrowIfCancellationRequested();

		    return await _store.GetAvailableBuildsAsync(cancellationToken);
	    }

		public async Task<string> GetLatestStableBuildAsync(CancellationToken cancellationToken = default)
        {
	        cancellationToken.ThrowIfCancellationRequested();

			if (!_options.Value.FetchArtifactsWhenMissing)
		        return (await _store.GetAvailableBuildsAsync(cancellationToken)).FirstOrDefault();

			return await _resolver.FetchLatestStableBuildAsync(cancellationToken);
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
				var resources = await _store.GetAvailableBuildsAsync(cancel);
				buildHash = resources.Contains(version) ? version : null;
			}

			return buildHash;
		}

        public async Task<byte[]> LoadBuildContentAsync(string buildHash, BuildFile buildFile, CancellationToken cancellationToken = default)
        {
	        return await _store.LoadBuildContentAsync(buildHash, buildFile, cancellationToken);
        }

        public async Task<bool> TryProvisionBuildAsync(string buildHash, CancellationToken cancellationToken = default)
        {
	        return await _store.TryProvisionBuildAsync(buildHash, cancellationToken);
        }
    }
}
