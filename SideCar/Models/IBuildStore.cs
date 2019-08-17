using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IBuildStore
	{
		Task<HashSet<string>> GetAvailableBuildsAsync(CancellationToken cancellationToken);
		Task<string> LoadBuildContentAsync(string buildHash, BuildFile buildFile, CancellationToken cancellationToken);
		Task<bool> TryProvisionBuildAsync(string buildHash, CancellationToken cancellationToken);
	}
}