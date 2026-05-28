using Microsoft.Extensions.Configuration;
using VisualMaster.Api.Configuration;

namespace VisualMaster.Api.Application
{
    public interface IApplicationBootstrapContext
    {
        IConfiguration Configuration { get; }

        ApplicationBootstrapOptions Application { get; }

        ModuleOptions Modules { get; }

        PathOptions Paths { get; }

        StartupOptions Startup { get; }

        string BaseDirectory { get; }
    }
}
