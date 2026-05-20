using VisualMaster.Api;
using VisualMaster.CameraLink.UI.ViewModels;
using System;
using System.Windows;

namespace VisualMaster.CameraLink.UI
{
    /// <summary>
    /// 独立的相机管理窗口，接受 <see cref="ICameraManager"/> 和 <see cref="CameraSystemConfig"/>
    /// 作为构造参数，以便在任意宿主应用中以对话框或独立窗口方式打开。
    /// </summary>
    public partial class CameraManagerWindow : Window
    {
        private readonly CameraManagerViewModel _viewModel;

        public CameraManagerWindow(ICameraManager manager, CameraSystemConfig config)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (config == null)  throw new ArgumentNullException(nameof(config));

            _viewModel = new CameraManagerViewModel(manager, config);

            InitializeComponent();

            Panel.DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel?.Dispose();
        }
    }
}
