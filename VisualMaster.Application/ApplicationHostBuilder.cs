using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VisualMaster.Api.Configuration;
using VisualMaster.Impl.Application;
using VisualMaster.Impl.DependencyInjection;

namespace VisualMaster.Application
{
    public static class ApplicationHostBuilder
    {
        public static IHost Build(string[] args)
        {
            return Create(args).Build();
        }

        public static IHostBuilder Create(string[] args)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configuration = BuildConfiguration(baseDirectory, args);
            var application = BindSection<ApplicationBootstrapOptions>(configuration, "Application");
            var modules = BindSection<ModuleOptions>(configuration, "Modules");
            var paths = BindSection<PathOptions>(configuration, "Paths");
            var startup = BindSection<StartupOptions>(configuration, "Startup");
            var bootstrapContext = new ApplicationBootstrapContext(
                configuration,
                application,
                modules,
                paths,
                startup,
                baseDirectory);

            return new HostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices(services =>
                {
                    services.AddApplicationCore(bootstrapContext);
                });
        }

        private static IConfigurationRoot BuildConfiguration(string baseDirectory, string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(baseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);

            if (args != null && args.Length > 0)
            {
                builder.AddCommandLine(args);
            }

            return builder.Build();
        }

        private static TOptions BindSection<TOptions>(IConfiguration configuration, string sectionName)
            where TOptions : new()
        {
            var options = new TOptions();
            configuration.GetSection(sectionName).Bind(options);
            return options;
        }
    }
}
