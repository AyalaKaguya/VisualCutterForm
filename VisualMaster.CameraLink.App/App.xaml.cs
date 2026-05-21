using VisualMaster.Api;
using VisualMaster.CameraLink;
using VisualMaster.CameraLink.UI;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace VisualMaster.CameraLink.App
{
    public partial class App : Application
    {
        private CameraManager _manager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 从命令行获取配置文件路径，缺省使用当前目录下的 camera_config.json
            string configArg = e.Args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
            string configPath = !string.IsNullOrEmpty(configArg)
                ? Path.GetFullPath(configArg)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "camera_config.json");

            // 加载或创建空配置
            var configFile = CameraConfigFile.Load(configPath);

            // 构建注入式配置容器
            var sysConfig = new CameraSystemConfig();
            sysConfig.LoadFrom(configFile.Cameras);

            // 保存委托：将当前设备列表写回文件
            sysConfig.SaveRequested += (s, _) =>
            {
                configFile.Cameras = sysConfig.Devices.Select(d => d.Clone()).ToList();
                try { configFile.Save(configPath); }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存配置失败：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            // 初始化相机管理器
            _manager = new CameraManager();
            _manager.Initialize();
            _manager.LoadConfig(sysConfig);

            if (e.Args.Any(a => string.Equals(a, "--test-image", StringComparison.OrdinalIgnoreCase)))
            {
                var testWindow = new ImageViewerTestWindow();
                testWindow.Closed += (s, _) => Shutdown();
                testWindow.Show();
                return;
            }

            // 打开相机管理窗口
            var window = new CameraManagerWindow(_manager, sysConfig);
            window.Title = string.IsNullOrEmpty(configPath)
                ? "相机管理 — 新建配置"
                : $"相机管理 — {Path.GetFileName(configPath)}";
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
