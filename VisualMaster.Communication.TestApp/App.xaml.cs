using System;
using System.IO;
using System.Linq;
using System.Windows;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;
using VisualMaster.Communication.UI;

namespace VisualMaster.Communication.TestApp
{
    public partial class App : Application
    {
        private CommunicationManager _manager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string configPath = e.Args.Length > 0
                ? Path.GetFullPath(e.Args[0])
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "communication_test_config.json");

            var file = CommunicationTestConfigFile.Load(configPath);
            var config = new CommunicationSystemConfig();
            config.LoadFrom(file.Devices);
            config.UpdateInputEvents(file.InputEvents);
            config.UpdateOutputEvents(file.OutputEvents);
            config.UpdateHeartbeats(file.Heartbeats);

            // ConfigSession-based save will replace SaveRequested
            /* config.SaveRequested += (s, _) =>
            {
                file.Devices = config.Devices.Select(d => d.Clone()).ToList();
                file.InputEvents = config.InputEvents.Select(d => d.Clone()).ToList();
                file.OutputEvents = config.OutputEvents.Select(d => d.Clone()).ToList();
                file.Heartbeats = config.Heartbeats.Select(d => d.Clone()).ToList();
                try { file.Save(configPath); }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存通信配置失败：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }; */

            _manager = new CommunicationManager();
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
