namespace VisualMaster.Api.Configuration
{
    public sealed class PathOptions
    {
        public string ConfigRoot { get; set; } = "config";

        public string DataRoot { get; set; } = "data";

        public string PluginRoot { get; set; } = "plugins";

        public string ProjectRoot { get; set; } = "projects";
    }
}
