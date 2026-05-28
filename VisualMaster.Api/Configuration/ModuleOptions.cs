namespace VisualMaster.Api.Configuration
{
    public sealed class ModuleOptions
    {
        public bool Camera { get; set; } = true;

        public bool Communication { get; set; } = true;

        public bool Operators { get; set; } = true;

        public bool Plugins { get; set; } = true;
    }
}
