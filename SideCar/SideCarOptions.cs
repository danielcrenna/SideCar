namespace SideCar
{
    public class SideCarOptions
    {
        public string ArtifactServer { get; set; }
        public string ArtifactMask { get; set; }
        public string BuildLocation { get; set; } = "builds";
        public string PackagesLocation { get; set; } = "output";
		public bool FetchArtifactsWhenMissing { get; set; } = true;
    }
}
