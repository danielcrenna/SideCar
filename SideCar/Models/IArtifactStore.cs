using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IArtifactStore
	{
		Task<HashSet<string>> GetAvailableArtifactsAsync(CancellationToken cancellationToken);
		Task<string> LoadBuildContentAsync(string buildHash, ArtifactFile artifactFile, CancellationToken cancellationToken);
	}
}