using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IArtifactResolver
	{
		Task<string> GetLatestStableBuildAsync(CancellationToken cancellationToken);
	}
}