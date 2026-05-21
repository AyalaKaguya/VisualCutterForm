using System;
using System.Windows;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.UI
{
    public partial class CommunicationManagerWindow : Window
    {
        public CommunicationManagerWindow(CommunicationManager manager, CommunicationSystemConfig config)
        {
            InitializeComponent();
            Root.Children.Add(new CommunicationManagerPanel(manager, config));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
