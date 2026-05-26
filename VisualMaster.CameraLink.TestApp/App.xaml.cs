using System;
using System.Windows;
using VisualMaster.CameraLink.UI;

namespace VisualMaster.CameraLink.TestApp.Viewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var window = new ImageViewerTestWindow
            {
                Title = "CameraLink 图像预览测试",
            };
            window.Closed += (s, _) => Shutdown();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
