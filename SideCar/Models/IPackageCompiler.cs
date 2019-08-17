using System.Reflection;
using System.Threading.Tasks;

namespace SideCar.Models
{
	public interface IPackageCompiler
	{
		Task<PackageResult> CompilePackageAsync(Assembly assembly, string buildHash);
	}
}