using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using VisualMaster.Api;

namespace VisualMaster.Forms
{
    public partial class SerialManagerForm : Form
    {
        private VisionController _vision;
        private SerialDeviceConfig _selectedDevice;

        public SerialManagerForm()
        {
            InitializeComponent();
        }

        public SerialManagerForm(VisionController vision) : this()
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "通信资源管理";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            _btnRefreshPorts.Click += (s, e) => RefreshPortList();
            _btnAddSlot.Click += (s, e) => AddDevice();
            _btnRemoveSlot.Click += (s, e) => RemoveSelectedDevice();
            _btnConnect.Click += (s, e) => ConnectSelectedDevice();
            _btnDisconnect.Click += (s, e) => DisconnectSelectedDevice();
            _slotListBox.SelectedIndexChanged += OnSlotSelected;
            _txtSlotName.TextChanged += OnDeviceNameChanged;
            _cmbPortName.TextChanged += OnDeviceConfigChanged;
            _cmbParity.SelectedIndexChanged += OnDeviceConfigChanged;
            _cmbStopBits.SelectedIndexChanged += OnDeviceConfigChanged;
            _numBaudRate.ValueChanged += OnDeviceConfigChanged;
            _numDataBits.ValueChanged += OnDeviceConfigChanged;
            Closing += (s, e) =>
            {
                foreach (var device in _vision.GetSerialDeviceConfigs())
                    _vision.DisconnectSerialDevice(device.DeviceId);
            };

            RefreshPortList();
            RefreshSlotList();
        }

        private void RefreshPortList()
        {
            _portListView.Items.Clear();
            _cmbPortName.Items.Clear();
            try
            {
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    bool open = _vision.IsSerialOpen(port);
                    var item = new ListViewItem(port);
                    item.SubItems.Add(open ? "已打开" : "空闲");
                    _portListView.Items.Add(item);
                }
                _cmbPortName.Items.AddRange(ports);
            }
            catch { }
            if (_cmbPortName.Items.Count > 0) _cmbPortName.SelectedIndex = 0;
        }

        private void RefreshSlotList()
        {
            _slotListBox.Items.Clear();
            foreach (var device in _vision.GetSerialDeviceConfigs())
            {
                var status = _vision.IsSerialDeviceConnected(device.DeviceId) ? " [已连接]" : " [未连接]";
                _slotListBox.Items.Add(new DisplayItem(device.DeviceId, $"{device.DisplayName}{status}") { Tag = device });
            }

            if (_selectedDevice != null)
            {
                for (int i = 0; i < _slotListBox.Items.Count; i++)
                {
                    var item = _slotListBox.Items[i] as DisplayItem;
                    if (item?.Id == _selectedDevice.DeviceId)
                    {
                        _slotListBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (_slotListBox.Items.Count > 0 && _selectedDevice == null)
                _slotListBox.SelectedIndex = 0;
        }

        private void AddDevice()
        {
            _selectedDevice = _vision.AddSerialDevice($"通信设备{_vision.GetSerialDeviceConfigs().Count + 1}", _cmbPortName.Text?.Trim());
            SyncDeviceEditors();
            RefreshSlotList();
        }

        private void ConnectSelectedDevice()
        {
            if (_selectedDevice == null)
                return;

            try
            {
                SaveSelectedDevice();
                _vision.ConnectSerialDevice(_selectedDevice.DeviceId);
                _selectedDevice = _vision.GetSerialDeviceConfig(_selectedDevice.DeviceId);
                RefreshPortList();
                RefreshSlotList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开串口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisconnectSelectedDevice()
        {
            if (_selectedDevice == null)
                return;

            _vision.DisconnectSerialDevice(_selectedDevice.DeviceId);
            RefreshPortList();
            RefreshSlotList();
        }

        private void RemoveSelectedDevice()
        {
            if (_selectedDevice == null)
                return;

            _vision.RemoveSerialDevice(_selectedDevice.DeviceId);
            _selectedDevice = null;
            RefreshPortList();
            RefreshSlotList();
            _btnConnect.Enabled = false;
            _btnDisconnect.Enabled = false;
            _btnRemoveSlot.Enabled = false;
        }

        private void OnSlotSelected(object sender, EventArgs e)
        {
            var item = _slotListBox.SelectedItem as DisplayItem;
            _selectedDevice = item?.Tag as SerialDeviceConfig;
            bool hasSel = _selectedDevice != null;
            bool isOpen = hasSel && _vision.IsSerialDeviceConnected(_selectedDevice.DeviceId);

            _btnRemoveSlot.Enabled = hasSel;
            _btnConnect.Enabled = hasSel && !isOpen;
            _btnDisconnect.Enabled = hasSel && isOpen;
            _btnAddSlot.Enabled = true;

            if (hasSel)
            {
                SyncDeviceEditors();
                _lblSlotStatus.Text = isOpen ? $"{_selectedDevice.DisplayName} 已连接" : $"{_selectedDevice.DisplayName} 未连接";
            }
            else
            {
                _txtSlotName.Text = "";
                _cmbPortName.Text = "";
                _lblSlotStatus.Text = "";
            }
        }

        private void OnDeviceNameChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null)
                return;

            SaveSelectedDevice();
            RefreshSlotList();
        }

        private void OnDeviceConfigChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null)
                return;

            SaveSelectedDevice();
            RefreshSlotList();
        }

        private void SaveSelectedDevice()
        {
            if (_selectedDevice == null)
                return;

            var updated = new SerialDeviceConfig
            {
                DeviceId = _selectedDevice.DeviceId,
                DisplayName = string.IsNullOrWhiteSpace(_txtSlotName.Text) ? _selectedDevice.DisplayName : _txtSlotName.Text.Trim(),
                PortName = _cmbPortName.Text?.Trim(),
                BaudRate = (int)_numBaudRate.Value,
                DataBits = (int)_numDataBits.Value,
                Parity = _cmbParity.Text,
                StopBits = _cmbStopBits.Text,
            };

            _vision.UpdateSerialDevice(updated);
            _selectedDevice = _vision.GetSerialDeviceConfig(updated.DeviceId);
        }

        private void SyncDeviceEditors()
        {
            if (_selectedDevice == null)
                return;

            _txtSlotName.Text = _selectedDevice.DisplayName ?? string.Empty;
            _cmbPortName.Text = _selectedDevice.PortName ?? string.Empty;
            _numBaudRate.Value = _selectedDevice.BaudRate;
            _numDataBits.Value = _selectedDevice.DataBits;
            _cmbParity.Text = string.IsNullOrWhiteSpace(_selectedDevice.Parity) ? "None" : _selectedDevice.Parity;
            _cmbStopBits.Text = string.IsNullOrWhiteSpace(_selectedDevice.StopBits) ? "One" : _selectedDevice.StopBits;
        }
    }
}
