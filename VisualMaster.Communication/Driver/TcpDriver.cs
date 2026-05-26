using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Driver
{
    public sealed class TcpDriver : CommunicationDriverBase
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CommunicationDeviceConfig _config;

        public override string DriverName => "TCP";

        public override void Initialize(CommunicationDeviceConfig config)
        {
            _config = config?.Clone() ?? throw new ArgumentNullException(nameof(config));
            base.Initialize(_config);
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (IsConnected) return;

            var settings = _config.DriverSettings ?? new System.Collections.Generic.Dictionary<string, string>();
            string ip = GetSetting(settings, "IpAddress", "127.0.0.1");
            int port = ParseInt(GetSetting(settings, "Port", "502"), 502);

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();
                IsConnected = true;
            }
            catch
            {
                if (_client != null)
                {
                    try { _client.Dispose(); } catch { }
                    _client = null;
                }
                _stream = null;
                IsConnected = false;
                throw;
            }
        }

        public override Task CloseAsync()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            try { _stream?.Dispose(); } catch { }
            try { _client?.Dispose(); } catch { }

            _stream = null;
            _client = null;
            IsConnected = false;
            return Task.CompletedTask;
        }

        protected override ICommunicationBlock CreateDriverBlock(CommunicationBlockConfig config)
        {
            var block = new CommunicationBlock(config);
            block.ReadHandler = async (timeout, token) =>
            {
                if (_stream == null || !_client.Connected)
                    throw new InvalidOperationException("TCP is not connected.");

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cts.CancelAfter(timeout);
                    var buffer = new byte[4096];
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token).ConfigureAwait(false);
                    if (read == 0) return new byte[0];
                    var result = new byte[read];
                    Array.Copy(buffer, result, read);
                    return result;
                }
            };
            block.WriteHandler = async (data, timeout, token) =>
            {
                if (_stream == null || !_client.Connected)
                    throw new InvalidOperationException("TCP is not connected.");
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cts.CancelAfter(timeout);
                    await _stream.WriteAsync(data ?? new byte[0], 0, data?.Length ?? 0, cts.Token).ConfigureAwait(false);
                }
            };
            return block;
        }

        private static string GetSetting(System.Collections.Generic.Dictionary<string, string> settings, string key, string fallback)
        {
            return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}
