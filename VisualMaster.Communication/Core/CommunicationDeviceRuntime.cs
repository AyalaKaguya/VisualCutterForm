using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Core
{
    internal sealed class CommunicationDeviceRuntime : IDisposable
    {
        private CancellationTokenSource _poller;

        public CommunicationDeviceRuntime(ICommunicationDriver driver, CommunicationDeviceConfig config)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            UpdateConfig(config);
        }

        public ICommunicationDriver Driver { get; }
        public CommunicationDeviceConfig Config { get; private set; }
        public CommunicationDeviceStatus Status { get; private set; }

        public event EventHandler<CommunicationBlockUpdatedEventArgs> BlockUpdated;
        public event EventHandler<CommunicationDeviceStatusChangedEventArgs> StatusChanged;

        public void UpdateConfig(CommunicationDeviceConfig config)
        {
            Config = config?.Clone() ?? throw new ArgumentNullException(nameof(config));

            foreach (var oldBlock in Driver.Blocks)
                oldBlock.Updated -= OnBlockUpdated;

            Driver.Initialize(Config);

            foreach (var newBlock in Driver.Blocks)
                newBlock.Updated += OnBlockUpdated;

            SyncStatusFromDriver();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Driver.IsEnabled)
            {
                SetStatus(CommunicationDeviceRuntimeState.Disabled, false, null);
                return;
            }

            try
            {
                SetStatus(CommunicationDeviceRuntimeState.Connecting, false, null);
                await Driver.ConnectAsync(cancellationToken).ConfigureAwait(false);
                StartPolling();
                SetStatus(CommunicationDeviceRuntimeState.Connected, true, null);
            }
            catch (Exception ex)
            {
                StopPolling();
                try { await Driver.CloseAsync().ConfigureAwait(false); } catch { }
                Driver.IsEnabled = false;
                Config.IsEnabled = false;
                SetStatus(CommunicationDeviceRuntimeState.Faulted, false, ex.Message);
                throw;
            }
        }

        public async Task StopAsync()
        {
            StopPolling();
            SetStatus(CommunicationDeviceRuntimeState.Disconnecting, false, null);
            await Driver.CloseAsync().ConfigureAwait(false);
            SetStatus(Driver.IsEnabled
                ? CommunicationDeviceRuntimeState.Disconnected
                : CommunicationDeviceRuntimeState.Disabled, false, null);
        }

        public ICommunicationBlock FindBlock(string blockId)
        {
            return Driver.Blocks.FirstOrDefault(b => b.Config.BlockId == blockId);
        }

        private void SyncStatusFromDriver()
        {
            var state = !Config.IsEnabled
                ? CommunicationDeviceRuntimeState.Disabled
                : Driver.IsConnected
                    ? CommunicationDeviceRuntimeState.Connected
                    : CommunicationDeviceRuntimeState.Disconnected;

            SetStatus(state, Driver.IsConnected && Config.IsEnabled, null);
        }

        private void StartPolling()
        {
            StopPolling();
            if (!Driver.Blocks.Any(b => b.Config.PollingEnabled)) return;

            var cts = new CancellationTokenSource();
            _poller = cts;
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var interval = Driver.Blocks.Where(b => b.Config.PollingEnabled)
                        .Select(b => Math.Max(50, b.Config.PollingIntervalMs))
                        .DefaultIfEmpty(500)
                        .Min();

                    try
                    {
                        await Driver.PollAsync(cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        SetStatus(CommunicationDeviceRuntimeState.Faulted, false, ex.Message);
                    }

                    try { await Task.Delay(interval, cts.Token).ConfigureAwait(false); }
                    catch { }
                }
            }, cts.Token);
        }

        private void StopPolling()
        {
            if (_poller == null) return;
            _poller.Cancel();
            _poller.Dispose();
            _poller = null;
        }

        private void OnBlockUpdated(object sender, CommunicationBlockUpdatedEventArgs e)
        {
            BlockUpdated?.Invoke(this, e);
        }

        private void SetStatus(CommunicationDeviceRuntimeState state, bool isConnected, string lastError)
        {
            Status = new CommunicationDeviceStatus(
                Driver.DeviceId,
                Config?.DriverName ?? Driver.DriverName,
                Config?.DisplayName,
                Config?.IsEnabled ?? Driver.IsEnabled,
                isConnected,
                state,
                lastError,
                DateTime.Now);

            StatusChanged?.Invoke(this, new CommunicationDeviceStatusChangedEventArgs(Status));
        }

        public void Dispose()
        {
            StopPolling();

            foreach (var block in Driver.Blocks)
                block.Updated -= OnBlockUpdated;

            Driver.Dispose();
        }
    }
}
