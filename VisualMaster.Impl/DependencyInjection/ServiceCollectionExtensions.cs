using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisualMaster.Api.Application;
using VisualMaster.Api.Configuration;
using VisualMaster.Api.FileSystem;
using VisualMaster.Impl.Configuration;
using VisualMaster.Impl.FileSystem;

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

            services.AddSingleton<IFileSystem, PhysicalFileSystem>();
            services.AddSingleton<IConfigurationSerializer, SystemTextJsonConfigurationSerializer>();
            services.AddSingleton<IApplicationConfigurationStore, JsonApplicationConfigurationStore>();

            return services;
        }

        public static IServiceCollection AddApplicationConfiguration<TConfiguration>(
            this IServiceCollection services)
            where TConfiguration : class, new()
        {
            services.AddSingleton(provider =>
                provider.GetRequiredService<IApplicationConfigurationStore>().LoadOrCreate<TConfiguration>());

            services.AddOptions<TConfiguration>()
                .Configure<IApplicationConfigurationStore>((options, store) => store.Apply(options));

            return services;
        }
    }
}
