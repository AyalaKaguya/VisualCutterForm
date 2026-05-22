using System;
using System.Windows;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class CommunicationManagerWindow : Window
    {
        private readonly CommunicationManagerViewModel _viewModel;

        public CommunicationManagerWindow(CommunicationManager manager, CommunicationSystemConfig config)
        {
            _viewModel = new CommunicationManagerViewModel(manager, config);
            InitializeComponent();
            Root.Children.Add(new CommunicationManagerPanel(_viewModel));
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
