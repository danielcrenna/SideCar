// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SideCar.Blazor
{
	public sealed class SideCarHost : ComponentBase, IDisposable
	{
		[Parameter] public Action Handler { get; set; }

		public void Dispose() { }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			Sequence.Begin(0, this);
			Handler();
		}
	}
}