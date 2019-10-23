#region LICENSE
// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#endregion

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SideCar.Blazor.Server
{
	[HtmlTargetElement("sidecar")]
	public class SideCarTagHelper : TagHelper
	{
		private readonly IHtmlHelper _htmlHelper;
		private readonly SideCarService _service;

		public SideCarTagHelper(IHtmlHelper htmlHelper, SideCarService service)
		{
			_htmlHelper = htmlHelper;
			_service = service;
		}

		[HtmlAttributeName("type")]
		public Type Type { get; set; }

		[HtmlAttributeName("renderMode")]
		public RenderMode RenderMode { get; set; }

		public object Parameters { get; set; }

		[HtmlAttributeNotBound]
		[ViewContext]
		public ViewContext ViewContext { get; set; }

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			((IViewContextAware) _htmlHelper).Contextualize(ViewContext);
			
			var renderComponentMethod = typeof(HtmlHelperComponentExtensions)
				.GetMethod(nameof(HtmlHelperComponentExtensions.RenderComponentAsync), new[] { typeof(IHtmlHelper), typeof(RenderMode), typeof(object) });

			var genericMethod = renderComponentMethod?.MakeGenericMethod(Type);

			var genericMethodAsync = (Task<IHtmlContent>) genericMethod?.Invoke(null, new[] {_htmlHelper, RenderMode, Parameters});
			if (genericMethodAsync == null)
				return;

			var content = await genericMethodAsync;
			if (content == null)
				return;

			output.PreContent.SetHtmlContent("<app>");

			output.Content.SetHtmlContent(content);

			var sb = new StringBuilder();
			sb.AppendLine("</app>");
			sb.AppendLine(await _service.RunAtAsync(RenderMode));
			output.PostContent.SetHtmlContent(sb.ToString());

			output.TagName = null;
		}
	}
}