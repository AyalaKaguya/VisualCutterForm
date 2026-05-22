using System;
using System.Windows.Controls;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class TcpDriverConfigControl : UserControl
    {
        private readonly TcpDriverConfigViewModel _viewModel;

        public event EventHandler ConfigChanged;

        public TcpDriverConfigControl(TcpDriverConfigViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            InitializeComponent();
            _viewModel.ConfigChanged += (s, e) => ConfigChanged?.Invoke(this, e);
        }
    }
}
