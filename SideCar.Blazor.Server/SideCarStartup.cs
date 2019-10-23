// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SideCar.Blazor.Server
{
	public class SideCarStartup<TClientStartup, TServerStartup> where TServerStartup : class
	{
		private readonly IConfiguration _configuration;
		private readonly TServerStartup _startup;

		public SideCarStartup(IConfiguration configuration)
		{
			_configuration = configuration;
			_startup = Activator.CreateInstance(typeof(TServerStartup), configuration) as TServerStartup;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			ServerConfigureServices(services);

			var configureAction = _configuration.GetSection("SideCar");
			services.Configure<SideCarOptions>(configureAction);

			var options = new SideCarOptions();
			configureAction.Bind(options);

			services.AddServerSideBlazor(o =>
			{
				o.DetailedErrors = Debugger.IsAttached;
			});

			switch (options.RunAt)
			{
				case RunAt.Server:
					services.AddScoped(s =>
						new HttpClient {BaseAddress = new Uri(s.GetRequiredService<NavigationManager>().BaseUri)});
					break;
				case RunAt.Client:
					// IMPORTANT: This is required if we make calls with an HttpClient outside of a component with a @page directive
					services.AddScoped(s => new HttpClient {BaseAddress = new Uri(s.GetRequiredService<NavigationManager>().BaseUri)});
					services.AddResponseCompression(o =>
					{
						var @default = ResponseCompressionDefaults.MimeTypes;
						o.MimeTypes = @default.Concat(new[] {MediaTypeNames.Application.Octet});
					});
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			ServerConfigure(app, env);

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			var options = app.ApplicationServices.GetRequiredService<IOptions<SideCarOptions>>();
			switch (options.Value.RunAt)
			{
				case RunAt.Server:
					break;
				case RunAt.Client:
					if (env.IsDevelopment())
						app.UseBlazorDebugging();
					app.UseResponseCompression();
					app.UseClientSideBlazorFiles<TClientStartup>();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			app.UseStaticFiles();
			app.UseRouting();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}

		private void ServerConfigureServices(IServiceCollection services)
		{
			var configureServices = _startup.GetType().GetMethods()
				.SingleOrDefault(x => x.Name == nameof(ConfigureServices));
			if (configureServices != null)
				configureServices.Invoke(_startup, new object[] {services});
		}

		private void ServerConfigure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			var configure = _startup.GetType().GetMethods().SingleOrDefault(x => x.Name == nameof(Configure));
			if (configure != null)
				configure.Invoke(_startup, new object[] {app, env});
		}
	}
}