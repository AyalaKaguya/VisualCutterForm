using System;
using System.IO.Ports;
using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public partial class UartDriverConfigControl : UserControl
    {
        private readonly CommunicationDeviceConfig _config;
        private bool _loading;

        public event EventHandler<CommunicationBlockConfig> RealtimeRequested;
        public event EventHandler ConfigChanged;

        public UartDriverConfigControl(CommunicationDeviceConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadConfig();
            InterfaceBox.TextChanged += (s, e) =>
            {
                _config.InterfaceName = InterfaceBox.Text;
                UpdateSettings();
            };
            PortBox.SelectionChanged += (s, e) => UpdateSettings();
            PortBox.LostFocus += (s, e) => UpdateSettings();
            BaudBox.SelectionChanged += (s, e) => UpdateSettings();
            BaudBox.LostFocus += (s, e) => UpdateSettings();
            DataBitsBox.SelectionChanged += (s, e) => UpdateSettings();
            ParityBox.SelectionChanged += (s, e) => UpdateSettings();
            StopBitsBox.SelectionChanged += (s, e) => UpdateSettings();
            HandshakeBox.SelectionChanged += (s, e) => UpdateSettings();
            BlockNameBox.TextChanged += (s, e) => UpdateBlock();
        }

        private void LoadConfig()
        {
            if (_config == null) return;
            _loading = true;
            try
            {
                EnsureDriverSettings();
                EnsureSingleBlock();
                foreach (var port in SerialPort.GetPortNames())
                    PortBox.Items.Add(port);

                InterfaceBox.Text = _config.InterfaceName ?? "";
                PortBox.Text = Get("PortName", _config.InterfaceName ?? "COM1");
                BaudBox.Text = Get("BaudRate", "9600");
                SelectText(DataBitsBox, Get("DataBits", "8"));
                SelectText(ParityBox, Get("Parity", "None"));
                SelectText(StopBitsBox, Get("StopBits", "One"));
                SelectText(HandshakeBox, Get("Handshake", "None"));

                var block = _config.Blocks[0];
                BlockNameBox.Text = string.IsNullOrWhiteSpace(block.Name) ? "串口数据" : block.Name;
                BlockAddressBox.Text = block.Address ?? "";
            }
            finally { _loading = false; }
        }

        private string Get(string key, string fallback)
        {
            return _config.DriverSettings != null && _config.DriverSettings.TryGetValue(key, out var value)
                ? value
                : fallback;
        }

        private static string SelectedText(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;
        }

        private void UpdateSettings()
        {
            if (_loading || _config == null) return;
            EnsureDriverSettings();
            _config.DriverSettings["PortName"] = PortBox.Text;
            _config.DriverSettings["BaudRate"] = BaudBox.Text;
            _config.DriverSettings["DataBits"] = SelectedText(DataBitsBox);
            _config.DriverSettings["Parity"] = SelectedText(ParityBox);
            _config.DriverSettings["StopBits"] = SelectedText(StopBitsBox);
            _config.DriverSettings["Handshake"] = SelectedText(HandshakeBox);
            _config.InterfaceName = string.IsNullOrWhiteSpace(InterfaceBox.Text) ? PortBox.Text : InterfaceBox.Text;
            UpdateBlockAddress();
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateBlock()
        {
            if (_loading || _config == null) return;
            EnsureSingleBlock();
            _config.Blocks[0].Name = string.IsNullOrWhiteSpace(BlockNameBox.Text) ? "串口数据" : BlockNameBox.Text.Trim();
            _config.Blocks[0].BlockName = _config.Blocks[0].Name;
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateBlockAddress()
        {
            EnsureSingleBlock();
            _config.Blocks[0].Address = $"{_config.DriverName}-{_config.InterfaceName}";
            BlockAddressBox.Text = _config.Blocks[0].Address;
        }

        private void OnRealtimeClick(object sender, System.Windows.RoutedEventArgs e)
        {
            EnsureSingleBlock();
            RealtimeRequested?.Invoke(this, _config.Blocks[0]);
        }

        private void EnsureDriverSettings()
        {
            if (_config.DriverSettings == null)
                _config.DriverSettings = new System.Collections.Generic.Dictionary<string, string>();
        }

        private void EnsureSingleBlock()
        {
            if (_config.Blocks == null)
                _config.Blocks = new System.Collections.Generic.List<CommunicationBlockConfig>();
            if (_config.Blocks.Count == 0)
            {
                _config.Blocks.Add(new CommunicationBlockConfig
                {
                    Name = "串口数据",
                    BlockName = "串口数据",
                    Address = $"{_config.DriverName}-{_config.InterfaceName}",
                    DataType = CommunicationBlockDataType.Bytes,
                });
            }
        }

        private static void SelectText(ComboBox combo, string text)
        {
            foreach (var item in combo.Items)
            {
                if ((item as ComboBoxItem)?.Content?.ToString() == text)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }
    }
}
