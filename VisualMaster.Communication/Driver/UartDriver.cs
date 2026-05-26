using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Driver
{
    public sealed class UartDriver : CommunicationDriverBase
    {
        private SerialPort _port;
        private CommunicationBlock _singleBlock;
        private CommunicationDeviceConfig _config;

        public override string DriverName => "UART";

        public override void Initialize(CommunicationDeviceConfig config)
        {
            _config = config?.Clone() ?? throw new ArgumentNullException(nameof(config));
            EnsureSingleBlock(_config);
            foreach (var block in _config.Blocks)
                block.PollingEnabled = false;
            base.Initialize(_config);
            _singleBlock = Blocks.OfType<CommunicationBlock>().FirstOrDefault();
        }

        public override Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (IsConnected) return Task.CompletedTask;

            var settings = _config.DriverSettings ?? new Dictionary<string, string>();
            string portName = GetSetting(settings, "PortName", "COM1");
            int baudRate = ParseInt(GetSetting(settings, "BaudRate", "9600"), 9600);
            int dataBits = ParseInt(GetSetting(settings, "DataBits", "8"), 8);
            Parity parity = ParseEnum(GetSetting(settings, "Parity", "None"), Parity.None);
            StopBits stopBits = ParseEnum(GetSetting(settings, "StopBits", "One"), StopBits.One);
            Handshake handshake = ParseEnum(GetSetting(settings, "Handshake", "None"), Handshake.None);

            if (string.IsNullOrWhiteSpace(portName))
                throw new InvalidOperationException("UART port is not configured.");

            if (stopBits == StopBits.OnePointFive && dataBits != 5)
                throw new InvalidOperationException("OnePointFive 停止位需要数据位为 5。");

            try
            {
                _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                _port.Handshake = handshake;
                _port.DataReceived += OnDataReceived;
                _port.Open();
                IsConnected = true;
            }
            catch
            {
                if (_port != null)
                {
                    _port.DataReceived -= OnDataReceived;
                    _port.Dispose();
                    _port = null;
                }
                IsConnected = false;
                throw;
            }
            return Task.CompletedTask;
        }

        public override Task CloseAsync()
        {
            if (_port == null) return Task.CompletedTask;
            try
            {
                _port.DataReceived -= OnDataReceived;
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                if (_port.IsOpen)
                    _port.Close();
            }
            catch { }
            finally
            {
                try { _port.Dispose(); } catch { }
                _port = null;
                IsConnected = false;
            }
            return Task.CompletedTask;
        }

        protected override ICommunicationBlock CreateDriverBlock(CommunicationBlockConfig config)
        {
            var block = new CommunicationBlock(config) { PublishOnWrite = false };
            block.ReadHandler = (timeout, token) => Task.FromResult(block.CurrentValue);
            block.WriteHandler = (data, timeout, token) =>
            {
                if (_port == null || !_port.IsOpen)
                    throw new InvalidOperationException("UART is not connected.");

                _port.Write(data ?? new byte[0], 0, data?.Length ?? 0);
                return Task.CompletedTask;
            };
            return block;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_port == null || _port.BytesToRead <= 0 || _singleBlock == null) return;
                var buffer = new byte[_port.BytesToRead];
                _port.Read(buffer, 0, buffer.Length);
                _singleBlock.Publish(buffer);
            }
            catch { }
        }

        private static void EnsureSingleBlock(CommunicationDeviceConfig config)
        {
            if (config.Blocks == null)
                config.Blocks = new List<CommunicationBlockConfig>();
            if (config.Blocks.Count > 0) return;

            string address = config.DriverSettings != null && config.DriverSettings.TryGetValue("PortName", out var port) ? port : "COM1";
            config.Blocks.Add(new CommunicationBlockConfig
            {
                Name = "UART Stream",
                BlockName = "",
                Address = address,
                DataType = CommunicationBlockDataType.Bytes,
                PollingEnabled = false,
            });
        }

        private static string GetSetting(Dictionary<string, string> settings, string key, string fallback)
        {
            return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
        }
    }
}
