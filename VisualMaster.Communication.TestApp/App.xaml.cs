using System;
using System.IO;
using System.Linq;
using System.Windows;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Config;
using VisualMaster.Communication.Core;
using VisualMaster.Communication.UI;

namespace VisualMaster.Communication.TestApp
{
    public partial class App : Application
    {
        private CommunicationManager _manager;
        private CommunicationConfigViewModel _configViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string configPath = e.Args.Length > 0
                ? Path.GetFullPath(e.Args[0])
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "communication_test_config.json");

            var file = CommunicationTestConfigFile.Load(configPath);
            var config = new CommunicationConfigSection();
            config.LoadFrom(file.Devices);
            config.UpdateInputEvents(file.InputEvents);
            config.UpdateOutputEvents(file.OutputEvents);
            config.UpdateHeartbeats(file.Heartbeats);

            var store = new JsonConfigStore(configPath);
            var configService = new CommunicationConfigService(store);
            configService.LoadConfigFrom(config);

            _manager = new CommunicationManager();
            configService.BindToManager(_manager);
            _configViewModel = configService.CreateViewModel();

            var window = new CommunicationManagerWindow(_manager, config)
            {
                Title = $"通信管理器测试 - {Path.GetFileName(configPath)}",
            };
            window.Closed += (s, _) => Shutdown();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _manager?.Dispose(); }
            catch { }
            base.OnExit(e);
        }
    }
}
