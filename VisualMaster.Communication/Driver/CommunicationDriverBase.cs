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
        public string InterfaceName { get; private set; }
        public bool IsEnabled { get; set; }
        public bool IsConnected { get; protected set; }
        public IReadOnlyList<ICommunicationBlock> Blocks => _blocks.AsReadOnly();

        public event EventHandler<CommunicationBlockUpdatedEventArgs> BlockUpdated;

        public virtual void Initialize(CommunicationDeviceConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            DeviceId = config.DeviceId;
            InterfaceName = config.InterfaceName;
            IsEnabled = config.IsEnabled;

            foreach (var block in _blocks.ToList())
                DetachBlock(block);
            _blocks.Clear();

            foreach (var blockConfig in config.Blocks)
                CreateBlock(blockConfig);
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
            AttachBlock(block);
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
            DetachBlock(block);
            _blocks.Remove(block);
        }

        public virtual UserControl CreateConfigurationView()
        {
            return new UserControl();
        }

        protected abstract ICommunicationBlock CreateDriverBlock(CommunicationBlockConfig config);

        protected void RaiseBlockUpdated(ICommunicationBlock block, byte[] data)
        {
            BlockUpdated?.Invoke(this,
                new CommunicationBlockUpdatedEventArgs(DeviceId, block.Config.BlockId, block.Config.Address, data));
        }

        private void AttachBlock(ICommunicationBlock block)
        {
            block.Updated += OnBlockUpdated;
        }

        private void DetachBlock(ICommunicationBlock block)
        {
            block.Updated -= OnBlockUpdated;
        }

        private void OnBlockUpdated(object sender, CommunicationBlockUpdatedEventArgs e)
        {
            var block = sender as ICommunicationBlock;
            if (block != null)
                RaiseBlockUpdated(block, e.Data);
        }

        public virtual void Dispose()
        {
            CloseAsync().GetAwaiter().GetResult();
        }
    }
}
