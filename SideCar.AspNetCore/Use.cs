using Microsoft.AspNetCore.Builder;

namespace SideCar.AspNetCore
{
	public static class Use
	{
		public static IApplicationBuilder UseSideCarApi(this IApplicationBuilder app)
		{
			app.UseMvc();
			return app;
		}
	}
}
