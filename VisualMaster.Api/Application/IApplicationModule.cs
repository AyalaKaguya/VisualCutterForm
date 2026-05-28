using Microsoft.Extensions.DependencyInjection;

namespace VisualMaster.Api.Application
{
    public interface IApplicationModule
    {
        string Name { get; }

        void ConfigureServices(IServiceCollection services, IApplicationBootstrapContext context);
    }
}
