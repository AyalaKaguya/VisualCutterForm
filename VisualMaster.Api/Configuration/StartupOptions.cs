namespace VisualMaster.Api.Configuration
{
    public sealed class StartupOptions
    {
        public bool LoadLastProject { get; set; } = true;

        public bool FailFastOnModuleError { get; set; }
    }
}
