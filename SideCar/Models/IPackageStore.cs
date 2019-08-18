using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IPackageStore
	{
		Task<Assembly> FindPackageAssemblyByNameAsync(string packageName, CancellationToken cancellationToken);
		Task<HashSet<string>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default);
		Task<byte[]> LoadPackageContentAsync(string packageHash, PackageFile packageFile, CancellationToken cancellationToken);
		Task<byte[]> LoadManagedLibraryAsync(string packageHash, string fileName, CancellationToken cancellationToken);
	}
}