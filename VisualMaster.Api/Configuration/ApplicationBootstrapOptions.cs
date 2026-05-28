namespace VisualMaster.Api.Configuration
{
    public sealed class ApplicationBootstrapOptions
    {
        public string Environment { get; set; } = "Production";

        public string Profile { get; set; } = "Default";

        public string Shell { get; set; } = "Wpf";

        public string Theme { get; set; } = "Dark";
    }
}
