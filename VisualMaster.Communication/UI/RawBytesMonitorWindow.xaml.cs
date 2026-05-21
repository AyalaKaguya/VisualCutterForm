using System;
using System.Windows;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.UI
{
    public partial class RawBytesMonitorWindow : Window
    {
        private readonly CommunicationManager _manager;
        private readonly string _deviceId;
        private readonly string _blockId;

        public RawBytesMonitorWindow(CommunicationManager manager, string deviceId, string blockId)
        {
            _manager = manager;
            _deviceId = deviceId;
            _blockId = blockId;
            InitializeComponent();
            _manager.BlockUpdated += OnBlockUpdated;
        }

        private void OnBlockUpdated(object sender, CommunicationBlockUpdatedEventArgs e)
        {
            if (e.DeviceId != _deviceId || e.BlockId != _blockId) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ShortMode.IsChecked == true)
                    ValueBox.Text = BitConverter.ToString(e.Data).Replace("-", " ");
                else
                    ValueBox.Text = CommunicationDataConverter.ToHex(e.Data);
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            _manager.BlockUpdated -= OnBlockUpdated;
            base.OnClosed(e);
        }
    }
}
