using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.UI
{
    public partial class RawBytesMonitorWindow : Window
    {
        private const int MaxHistoryLines = 100;

        private readonly CommunicationManager _manager;
        private readonly string _deviceId;
        private readonly string _blockId;
        private byte[] _lastData;

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
            if (_lastData != null && DataEqual(e.Data, _lastData)) return;
            _lastData = (byte[])e.Data.Clone();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string formatted;
                if (ShortMode.IsChecked == true)
                    formatted = BitConverter.ToString(e.Data).Replace("-", " ");
                else if (StringMode.IsChecked == true)
                    formatted = Encoding.ASCII.GetString(e.Data);
                else
                    formatted = CommunicationDataConverter.ToHex(e.Data);

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                ValueBox.AppendText($"[{timestamp}] -> {formatted}\n");
                PruneHistory();
                ValueBox.ScrollToEnd();
            }));
        }

        private static bool DataEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private void PruneHistory()
        {
            var lines = ValueBox.Text.Split('\n');
            if (lines.Length <= MaxHistoryLines) return;
            ValueBox.Text = string.Join("\n", lines.Skip(lines.Length - MaxHistoryLines));
        }

        private async void SendData()
        {
            string text = SendBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            byte[] data;
            if (SendStringMode.IsChecked == true)
                data = Encoding.ASCII.GetBytes(text);
            else
                data = CommunicationDataConverter.FromHex(text);

            await _manager.WriteBlockAsync(_deviceId, _blockId, data);
        }

        private void SendBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendData();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendData();
        }

        protected override void OnClosed(EventArgs e)
        {
            _manager.BlockUpdated -= OnBlockUpdated;
            base.OnClosed(e);
        }
    }
}
