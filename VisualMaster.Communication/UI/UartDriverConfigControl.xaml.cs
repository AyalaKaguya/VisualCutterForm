using System;
using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class UartDriverConfigControl : UserControl
    {
        private readonly UartDriverConfigViewModel _viewModel;

        public event EventHandler RealtimeRequested;
        public event EventHandler ConfigChanged;

        public UartDriverConfigControl(UartDriverConfigViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            InitializeComponent();
            _viewModel.ConfigChanged += (s, e) => ConfigChanged?.Invoke(this, e);
        }

        private void OnRealtimeClick(object sender, System.Windows.RoutedEventArgs e)
        {
            RealtimeRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
