using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.UI
{
    public partial class RawBytesMonitorWindow : Window
    {
        private const int MaxHistoryLines = 100;

        private readonly ICommunicationBlock _block;

        public RawBytesMonitorWindow(ICommunicationBlock block)
        {
            _block = block ?? throw new ArgumentNullException(nameof(block));
            InitializeComponent();
            _block.Updated += OnBlockUpdated;
        }

        private void OnBlockUpdated(object sender, CommunicationBlockUpdatedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string formatted;
                if (ShortMode.IsChecked == true)
                    formatted = string.Join(" ", e.Data.Select(b => b.ToString()));
                else if (StringMode.IsChecked == true)
                    formatted = Encoding.ASCII.GetString(e.Data);
                else
                    formatted = CommunicationDataConverter.ToHex(e.Data);

                AppendLog(formatted, "<-");
            }));
        }

        private void AppendLog(string data, string arrow)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            ValueBox.AppendText($"[{timestamp}] {arrow} {data}\n");
            PruneHistory();
            ValueBox.ScrollToEnd();
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

            await _block.WriteAsync(data, 1000, CancellationToken.None);
            _ = Dispatcher.BeginInvoke(new Action(() => AppendLog(text, "->")));
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
            _block.Updated -= OnBlockUpdated;
            base.OnClosed(e);
        }
    }
}
