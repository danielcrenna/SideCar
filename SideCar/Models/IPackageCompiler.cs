using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IPackageCompiler
	{
		Task<PackageResult> CompilePackageAsync(Assembly assembly, string buildHash, CancellationToken cancellationToken);
	}
}