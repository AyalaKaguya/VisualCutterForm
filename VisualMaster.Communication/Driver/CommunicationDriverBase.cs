using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Driver
{
    public abstract class CommunicationDriverBase : ICommunicationDriver
    {
        private readonly List<ICommunicationBlock> _blocks = new List<ICommunicationBlock>();

        public string DeviceId { get; private set; }
        public abstract string DriverName { get; }
        public bool IsEnabled { get; set; }
        public bool IsConnected { get; protected set; }
        public IReadOnlyList<ICommunicationBlock> Blocks => _blocks.AsReadOnly();

        public virtual void Initialize(CommunicationDeviceConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            DeviceId = config.DeviceId;
            IsEnabled = config.IsEnabled;

            var configBlockIds = new HashSet<string>(config.Blocks.Select(b => b.BlockId));
            foreach (var existing in _blocks.ToList())
            {
                if (!configBlockIds.Contains(existing.Config.BlockId))
                    _blocks.Remove(existing);
            }

            foreach (var blockConfig in config.Blocks)
                UpdateBlock(blockConfig);
        }

        public abstract Task ConnectAsync(CancellationToken cancellationToken);

        public virtual async Task PollAsync(CancellationToken cancellationToken)
        {
            foreach (var block in Blocks.Where(b => b.Config.PollingEnabled))
                await block.ReadAsync(block.Config.PollingTimeoutMs, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task ReconnectAsync(CancellationToken cancellationToken)
        {
            await CloseAsync().ConfigureAwait(false);
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        public abstract Task CloseAsync();

        public virtual ICommunicationBlock CreateBlock(CommunicationBlockConfig config)
        {
            var block = CreateDriverBlock(config);
            if (block is CommunicationBlock cb)
                cb.DeviceId = DeviceId;
            _blocks.Add(block);
            return block;
        }

        public virtual void UpdateBlock(CommunicationBlockConfig config)
        {
            var block = _blocks.FirstOrDefault(b => b.Config.BlockId == config.BlockId);
            if (block == null)
                CreateBlock(config);
            else
                block.UpdateConfig(config);
        }

        public virtual void RemoveBlock(string blockId)
        {
            var block = _blocks.FirstOrDefault(b => b.Config.BlockId == blockId);
            if (block == null) return;
            _blocks.Remove(block);
        }

        public virtual UserControl CreateConfigurationView()
        {
            return new UserControl();
        }

        protected abstract ICommunicationBlock CreateDriverBlock(CommunicationBlockConfig config);

        public virtual void Dispose()
        {
            CloseAsync().GetAwaiter().GetResult();
        }
    }
}
