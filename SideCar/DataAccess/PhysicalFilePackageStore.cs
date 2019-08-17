using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SideCar.Models;

namespace SideCar.DataAccess
{
	public class PhysicalFilePackageStore : IPackageStore
	{
		private readonly ILogger<PhysicalFileArtifactStore> _logger;

		public PhysicalFilePackageStore(ILogger<PhysicalFileArtifactStore> logger)
		{
			_logger = logger;
		}

		public Assembly FindPackageAssemblyByName(string packageName)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly target = null;
			foreach (var assembly in assemblies)
			{
				var name = assembly.GetName().Name;
				if (!name.Equals(packageName, StringComparison.OrdinalIgnoreCase))
					continue;
				target = assembly;
				break;
			}
			return target;
		}
	}
}