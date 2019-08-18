// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
