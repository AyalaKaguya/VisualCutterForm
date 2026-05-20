using VisualMaster.CameraLink.UI.ViewModels;
using System.Windows.Controls;

namespace VisualMaster.CameraLink.UI
{
    /// <summary>
    /// 相机管理 UserControl——可插入任何 WPF 宿主界面。
    /// 通过构造函数接收 ViewModel，或由外部直接设置 DataContext。
    /// </summary>
    public partial class CameraManagerPanel : UserControl
    {
        public CameraManagerPanel()
        {
            InitializeComponent();
        }

        public CameraManagerPanel(CameraManagerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
