using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisualMaster.Api.Application;
using VisualMaster.Api.Configuration;

namespace VisualMaster.Impl.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationCore(
            this IServiceCollection services,
            IApplicationBootstrapContext context)
        {
            services.AddSingleton(context);
            services.AddSingleton(context.Configuration);
            services.AddSingleton(context.Application);
            services.AddSingleton(context.Modules);
            services.AddSingleton(context.Paths);
            services.AddSingleton(context.Startup);

            services.Configure<ApplicationBootstrapOptions>(context.Configuration.GetSection("Application"));
            services.Configure<ModuleOptions>(context.Configuration.GetSection("Modules"));
            services.Configure<PathOptions>(context.Configuration.GetSection("Paths"));
            services.Configure<StartupOptions>(context.Configuration.GetSection("Startup"));

            return services;
        }
    }
}
