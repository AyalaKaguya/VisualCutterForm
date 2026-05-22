using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Driver;

namespace VisualMaster.Communication.Core
{
    public sealed class CommunicationManager : IDisposable
    {
        private readonly Dictionary<string, ICommunicationDriverFactory> _factories =
            new Dictionary<string, ICommunicationDriverFactory>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ICommunicationDriver> _drivers =
            new Dictionary<string, ICommunicationDriver>();
        private readonly Dictionary<string, CancellationTokenSource> _pollers =
            new Dictionary<string, CancellationTokenSource>();
        private readonly Dictionary<string, byte[]> _previousValues =
            new Dictionary<string, byte[]>();
        private readonly CommunicationInputEvaluator _inputEvaluator = new CommunicationInputEvaluator();
        private readonly CommunicationOutputBuilder _outputBuilder = new CommunicationOutputBuilder();
        private CommunicationSystemConfig _config;

        public CommunicationManager()
        {
            RegisterDriver(new UartDriverFactory());
            RegisterDriver(new TcpDriverFactory());
        }

        public IReadOnlyList<ICommunicationDriverFactory> DriverFactories => _factories.Values.ToList();
        public IReadOnlyList<ICommunicationDriver> Drivers => _drivers.Values.ToList();

        public event EventHandler<CommunicationInputEventConfig> InputEventTriggered;
        public event EventHandler<string> StatusChanged;

        public void RegisterDriver(ICommunicationDriverFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[factory.DriverName] = factory;
        }

        public void LoadConfig(CommunicationSystemConfig config)
        {
            if (_config != null)
            {
                _config.DeviceAdded -= OnDeviceAdded;
                _config.DeviceUpdated -= OnDeviceUpdated;
                _config.DeviceRemoved -= OnDeviceRemoved;
                _config.Reset -= OnConfigReset;
            }

            _config = config;
            if (_config == null) return;

            SyncFromConfig();
            _config.DeviceAdded += OnDeviceAdded;
            _config.DeviceUpdated += OnDeviceUpdated;
            _config.DeviceRemoved += OnDeviceRemoved;
            _config.Reset += OnConfigReset;
        }

        public CommunicationDeviceConfig AddDevice(string driverName)
        {
            if (!_factories.TryGetValue(driverName, out var factory))
                throw new InvalidOperationException($"Driver is not registered: {driverName}");

            var existing = _drivers.Values.ToList();
            var config = factory.CreateDefaultConfig(existing as IReadOnlyList<ICommunicationDriver>);
            if (_config != null)
            {
                var added = _config.AddDevice(config.DriverName, config.DisplayName);
                config.DeviceId = added.DeviceId;
                _config.UpdateDevice(config);
            }
            else
            {
                CreateOrUpdateDriver(config);
            }

            return config.Clone();
        }

        public async Task StartDeviceAsync(string deviceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_drivers.TryGetValue(deviceId, out var driver) || !driver.IsEnabled)
                return;

            try
            {
                await StartDriverAsync(driver, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try { await driver.CloseAsync().ConfigureAwait(false); } catch { }
                driver.IsEnabled = false;
                throw;
            }
        }

        public async Task StartAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var driver in _drivers.Values.Where(d => d.IsEnabled))
                await StartDriverAsync(driver, cancellationToken).ConfigureAwait(false);
        }

        public async Task StopDeviceAsync(string deviceId)
        {
            StopPolling(deviceId);
            if (_drivers.TryGetValue(deviceId, out var driver))
                await driver.CloseAsync().ConfigureAwait(false);
        }

        public async Task StopAllAsync()
        {
            foreach (var id in _drivers.Keys.ToList())
                await StopDeviceAsync(id).ConfigureAwait(false);
        }

        public async Task WriteBlockAsync(string deviceId, string blockId, byte[] data, int timeoutMs = 1000)
        {
            var block = FindBlock(deviceId, blockId);
            if (block == null)
                throw new InvalidOperationException($"Block not found: {deviceId}/{blockId}");
            await block.WriteAsync(data ?? new byte[0], timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<byte[]> ReadBlockAsync(string deviceId, string blockId, int timeoutMs = 1000)
        {
            var block = FindBlock(deviceId, blockId);
            if (block == null)
                throw new InvalidOperationException($"Block not found: {deviceId}/{blockId}");
            return await block.ReadAsync(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task ExecuteOutputEventAsync(string eventId, IDictionary<string, string> variables = null)
        {
            var output = _config?.OutputEvents.FirstOrDefault(e => e.EventId == eventId);
            if (output == null) return;
            var data = _outputBuilder.Build(output, variables ?? new Dictionary<string, string>());
            await WriteBlockAsync(output.DeviceId, output.BlockId, data).ConfigureAwait(false);
        }

        public ICommunicationBlock FindBlock(string deviceId, string blockId)
        {
            return _drivers.TryGetValue(deviceId, out var driver)
                ? driver.Blocks.FirstOrDefault(b => b.Config.BlockId == blockId)
                : null;
        }

        public IReadOnlyDictionary<(string deviceId, string blockId), ICommunicationBlock> BlockLookup
        {
            get
            {
                var dict = new Dictionary<(string, string), ICommunicationBlock>();
                foreach (var driver in _drivers.Values)
                foreach (var block in driver.Blocks)
                    dict[(driver.DeviceId, block.Config.BlockId)] = block;
                return dict;
            }
        }

        public CommunicationDeviceConfig GetDeviceConfig(string deviceId)
        {
            return _config?.GetDevice(deviceId);
        }

        private async Task StartDriverAsync(ICommunicationDriver driver, CancellationToken cancellationToken)
        {
            await driver.ConnectAsync(cancellationToken).ConfigureAwait(false);
            StartPolling(driver);
            StatusChanged?.Invoke(this, $"{driver.DriverName} connected.");
        }

        private void SyncFromConfig()
        {
            var ids = new HashSet<string>(_config.Devices.Select(d => d.DeviceId));
            foreach (var id in _drivers.Keys.ToList())
            {
                if (!ids.Contains(id))
                    RemoveDriver(id);
            }

            foreach (var device in _config.Devices)
                CreateOrUpdateDriver(device);
        }

        private void CreateOrUpdateDriver(CommunicationDeviceConfig config)
        {
            if (!_factories.TryGetValue(config.DriverName, out var factory))
                return;

            if (_drivers.TryGetValue(config.DeviceId, out var existing))
            {
                foreach (var oldBlock in existing.Blocks)
                    oldBlock.Updated -= OnBlockForInputEvents;

                existing.Initialize(config);

                foreach (var newBlock in existing.Blocks)
                    newBlock.Updated += OnBlockForInputEvents;
                return;
            }

            var driver = factory.CreateDriver();
            driver.Initialize(config);

            foreach (var block in driver.Blocks)
                block.Updated += OnBlockForInputEvents;

            _drivers[config.DeviceId] = driver;
        }

        private void RemoveDriver(string deviceId)
        {
            StopPolling(deviceId);
            if (!_drivers.TryGetValue(deviceId, out var driver)) return;

            foreach (var block in driver.Blocks)
                block.Updated -= OnBlockForInputEvents;

            driver.Dispose();
            _drivers.Remove(deviceId);
        }

        private void OnBlockForInputEvents(object sender, CommunicationBlockUpdatedEventArgs e)
        {
            ProcessInputEvents(e);
        }

        private void StartPolling(ICommunicationDriver driver)
        {
            StopPolling(driver.DeviceId);
            if (!driver.Blocks.Any(b => b.Config.PollingEnabled)) return;

            var cts = new CancellationTokenSource();
            _pollers[driver.DeviceId] = cts;
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var interval = driver.Blocks.Where(b => b.Config.PollingEnabled)
                        .Select(b => Math.Max(50, b.Config.PollingIntervalMs))
                        .DefaultIfEmpty(500)
                        .Min();

                    try { await driver.PollAsync(cts.Token).ConfigureAwait(false); }
                    catch { }
                    try { await Task.Delay(interval, cts.Token).ConfigureAwait(false); }
                    catch { }
                }
            }, cts.Token);
        }

        private void StopPolling(string deviceId)
        {
            if (!_pollers.TryGetValue(deviceId, out var cts)) return;
            cts.Cancel();
            cts.Dispose();
            _pollers.Remove(deviceId);
        }

        private void ProcessInputEvents(CommunicationBlockUpdatedEventArgs e)
        {
            if (_config == null) return;
            string key = $"{e.DeviceId}|{e.BlockId}";
            _previousValues.TryGetValue(key, out var previous);

            foreach (var input in _config.InputEvents.Where(i => i.DeviceId == e.DeviceId && i.BlockId == e.BlockId))
            {
                if (!_inputEvaluator.Matches(input, e.Data, previous))
                    continue;

                InputEventTriggered?.Invoke(this, input);
                foreach (var heartbeat in _config.Heartbeats.Where(h => h.IsEnabled && h.InputEventId == input.EventId))
                    ExecuteOutputEventAsync(heartbeat.OutputEventId).GetAwaiter().GetResult();
            }

            _previousValues[key] = e.Data != null ? (byte[])e.Data.Clone() : new byte[0];
        }

        private void OnDeviceAdded(object sender, CommunicationDeviceConfig e) => CreateOrUpdateDriver(e);
        private void OnDeviceUpdated(object sender, CommunicationDeviceConfig e) => CreateOrUpdateDriver(e);
        private void OnDeviceRemoved(object sender, string e) => RemoveDriver(e);
        private void OnConfigReset(object sender, EventArgs e) => SyncFromConfig();

        public void Dispose()
        {
            StopAllAsync().GetAwaiter().GetResult();
            foreach (var driver in _drivers.Values)
            {
                foreach (var block in driver.Blocks)
                    block.Updated -= OnBlockForInputEvents;
                driver.Dispose();
            }
            _drivers.Clear();
        }
    }
}
