using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IBuildResolver
	{
		Task<string> FetchLatestStableBuildAsync(CancellationToken cancellationToken);
	}
}