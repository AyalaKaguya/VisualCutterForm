using Microsoft.Extensions.Configuration;
using VisualMaster.Api.Application;
using VisualMaster.Api.Configuration;

namespace VisualMaster.Impl.Application
{
    public sealed class ApplicationBootstrapContext : IApplicationBootstrapContext
    {
        public ApplicationBootstrapContext(
            IConfiguration configuration,
            ApplicationBootstrapOptions application,
            ModuleOptions modules,
            PathOptions paths,
            StartupOptions startup,
            string baseDirectory)
        {
            Configuration = configuration;
            Application = application;
            Modules = modules;
            Paths = paths;
            Startup = startup;
            BaseDirectory = baseDirectory;
        }

        public IConfiguration Configuration { get; }

        public ApplicationBootstrapOptions Application { get; }

        public ModuleOptions Modules { get; }

        public PathOptions Paths { get; }

        public StartupOptions Startup { get; }

        public string BaseDirectory { get; }
    }
}
