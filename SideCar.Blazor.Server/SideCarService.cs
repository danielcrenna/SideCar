using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace SideCar.Blazor.Server
{
	public class SideCarService
	{
		private readonly IOptionsMonitor<SideCarOptions> _options;

		public SideCarService(IOptionsMonitor<SideCarOptions> options)
		{
			_options = options;
		}

		public Task<string> RunAtAsync(RenderMode renderMode)
		{
			switch(_options.CurrentValue.RunAt)
			{
				case RunAt.Server:
					return Task.FromResult("<script src=\"_framework/blazor.server.js\"></script>");
				case RunAt.Client:
					return Task.FromResult("<script src=\"_framework/blazor.webassembly.js\"></script>");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
