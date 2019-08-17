using System.Reflection;

namespace SideCar.Models
{
	public interface IPackageStore
	{
		Assembly FindPackageAssemblyByName(string packageName);
	}
}