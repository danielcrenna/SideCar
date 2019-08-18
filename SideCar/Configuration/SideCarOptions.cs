// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace SideCar.Configuration
{
    public class SideCarOptions
    {
        public string ArtifactServer { get; set; } = "https://jenkins.mono-project.com/job/test-mono-mainline-wasm/lastStableBuild/";
		public string ArtifactMask { get; set; } = "https://xamjenkinsartifact.azureedge.net/test-mono-mainline-wasm/{0}/ubuntu-1804-amd64/sdks/wasm/mono-wasm-{1}.zip";
		public string BuildLocation { get; set; } = "builds";
        public string PackageLocation { get; set; } = "output";
		public bool FetchArtifactsWhenMissing { get; set; } = true;
    }
}
