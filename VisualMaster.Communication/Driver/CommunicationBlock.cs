using System;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Driver
{
    public class CommunicationBlock : ICommunicationBlock
    {
        private byte[] _currentValue;

        public CommunicationBlock(CommunicationBlockConfig config)
        {
            Config = config?.Clone() ?? throw new ArgumentNullException(nameof(config));
            _currentValue = Config.InitialValue != null ? (byte[])Config.InitialValue.Clone() : new byte[0];
        }

        public CommunicationBlockConfig Config { get; private set; }
        public byte[] CurrentValue => _currentValue != null ? (byte[])_currentValue.Clone() : new byte[0];

        public event EventHandler<CommunicationBlockUpdatedEventArgs> Updated;

        public Func<int, CancellationToken, Task<byte[]>> ReadHandler { get; set; }
        public Func<byte[], int, CancellationToken, Task> WriteHandler { get; set; }
        public bool PublishOnWrite { get; set; } = true;

        public async Task<byte[]> ReadAsync(int timeoutMs, CancellationToken cancellationToken)
        {
            if (ReadHandler == null)
                return CurrentValue;

            var data = await ReadHandler(timeoutMs, cancellationToken).ConfigureAwait(false);
            Publish(data);
            return CurrentValue;
        }

        public async Task WriteAsync(byte[] data, int timeoutMs, CancellationToken cancellationToken)
        {
            if (WriteHandler == null)
            {
                SetValue(data);
                return;
            }

            await WriteHandler(data ?? new byte[0], timeoutMs, cancellationToken).ConfigureAwait(false);
            SetValue(data);
        }

        private void SetValue(byte[] data)
        {
            _currentValue = data != null ? (byte[])data.Clone() : new byte[0];
            if (PublishOnWrite)
                Updated?.Invoke(this, new CommunicationBlockUpdatedEventArgs(null, Config.BlockId, Config.Address, _currentValue));
        }

        public void UpdateConfig(CommunicationBlockConfig config)
        {
            if (config == null) return;
            Config = config.Clone();
        }

        public void Publish(byte[] data)
        {
            _currentValue = data != null ? (byte[])data.Clone() : new byte[0];
            Updated?.Invoke(this, new CommunicationBlockUpdatedEventArgs(null, Config.BlockId, Config.Address, _currentValue));
        }
    }
}
