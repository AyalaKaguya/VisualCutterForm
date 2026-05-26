using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Config;
using VisualMaster.Communication.Driver;

namespace VisualMaster.Communication.Core
{
    public sealed class CommunicationManager : IDisposable
    {
        private readonly Dictionary<string, ICommunicationDriverFactory> _factories =
            new Dictionary<string, ICommunicationDriverFactory>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CommunicationDeviceRuntime> _runtimes =
            new Dictionary<string, CommunicationDeviceRuntime>();
        private readonly Dictionary<string, byte[]> _previousValues =
            new Dictionary<string, byte[]>();
        private readonly CommunicationInputEvaluator _inputEvaluator = new CommunicationInputEvaluator();
        private readonly CommunicationOutputBuilder _outputBuilder = new CommunicationOutputBuilder();
        private CommunicationConfigSection _config;

        public CommunicationManager()
        {
            RegisterDriver(new UartDriverFactory());
            RegisterDriver(new TcpDriverFactory());
        }

        public IReadOnlyList<ICommunicationDriverFactory> DriverFactories => _factories.Values.ToList();
        public IReadOnlyList<ICommunicationDriver> Drivers => _runtimes.Values.Select(r => r.Driver).ToList();
        public IReadOnlyList<CommunicationDeviceStatus> DeviceStatuses => _runtimes.Values
            .Select(r => r.Status)
            .Where(s => s != null)
            .ToList();

        public event EventHandler<CommunicationInputEventConfig> InputEventTriggered;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<CommunicationDeviceStatusChangedEventArgs> DeviceStatusChanged;

        public void RegisterDriver(ICommunicationDriverFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[factory.DriverName] = factory;
        }

        public void LoadConfig(CommunicationConfigSection config)
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

            var existing = Drivers.ToList();
            var config = factory.CreateDefaultConfig(existing as IReadOnlyList<ICommunicationDriver>);
            if (_config != null)
            {
                var added = _config.AddDevice(config.DriverName, config.DisplayName);
                config.DeviceId = added.DeviceId;
                _config.UpdateDevice(config);
            }
            else
            {
                CreateOrUpdateRuntime(config);
            }

            return config.Clone();
        }

        public async Task StartDeviceAsync(string deviceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_runtimes.TryGetValue(deviceId, out var runtime) || !runtime.Driver.IsEnabled)
            {
                return;
            }

            try
            {
                await runtime.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                var failedConfig = runtime.Config?.Clone() ?? _config?.GetDevice(deviceId);
                if (failedConfig != null)
                {
                    failedConfig.IsEnabled = false;
                    _config.UpdateDevice(failedConfig);
                }
                throw;
            }
        }

        public async Task StartAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var runtime in _runtimes.Values.Where(r => r.Driver.IsEnabled).ToList())
            {
                try
                {
                    await StartDeviceAsync(runtime.Driver.DeviceId, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }

        public async Task StopDeviceAsync(string deviceId)
        {
            if (_runtimes.TryGetValue(deviceId, out var runtime))
                await runtime.StopAsync().ConfigureAwait(false);
        }

        public async Task StopAllAsync()
        {
            foreach (var id in _runtimes.Keys.ToList())
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
            return _runtimes.TryGetValue(deviceId, out var runtime)
                ? runtime.FindBlock(blockId)
                : null;
        }

        public IReadOnlyDictionary<(string deviceId, string blockId), ICommunicationBlock> BlockLookup
        {
            get
            {
                var dict = new Dictionary<(string, string), ICommunicationBlock>();
                foreach (var runtime in _runtimes.Values)
                foreach (var block in runtime.Driver.Blocks)
                    dict[(runtime.Driver.DeviceId, block.Config.BlockId)] = block;
                return dict;
            }
        }

        public CommunicationDeviceConfig GetDeviceConfig(string deviceId)
        {
            return _config?.GetDevice(deviceId);
        }

        public CommunicationDeviceStatus GetDeviceStatus(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;
            return _runtimes.TryGetValue(deviceId, out var runtime) ? runtime.Status : null;
        }

        private void SyncFromConfig()
        {
            var ids = new HashSet<string>(_config.Devices.Select(d => d.DeviceId));
            foreach (var id in _runtimes.Keys.ToList())
            {
                if (!ids.Contains(id))
                    RemoveRuntime(id);
            }

            foreach (var device in _config.Devices)
                CreateOrUpdateRuntime(device);
        }

        private void CreateOrUpdateRuntime(CommunicationDeviceConfig config)
        {
            if (!_factories.TryGetValue(config.DriverName, out var factory))
                return;

            if (_runtimes.TryGetValue(config.DeviceId, out var existing))
            {
                if (!string.Equals(existing.Driver.DriverName, config.DriverName, StringComparison.OrdinalIgnoreCase))
                {
                    RemoveRuntime(config.DeviceId);
                }
                else
                {
                    existing.UpdateConfig(config);
                    return;
                }
            }

            var driver = factory.CreateDriver();
            var runtime = new CommunicationDeviceRuntime(driver, config);
            runtime.BlockUpdated += OnBlockForInputEvents;
            runtime.StatusChanged += OnRuntimeStatusChanged;
            _runtimes[config.DeviceId] = runtime;
        }

        private void RemoveRuntime(string deviceId)
        {
            if (!_runtimes.TryGetValue(deviceId, out var runtime)) return;
            runtime.BlockUpdated -= OnBlockForInputEvents;
            runtime.StatusChanged -= OnRuntimeStatusChanged;
            runtime.Dispose();
            _runtimes.Remove(deviceId);
        }

        private void OnRuntimeStatusChanged(object sender, CommunicationDeviceStatusChangedEventArgs e)
        {
            DeviceStatusChanged?.Invoke(this, e);

            if (!string.IsNullOrEmpty(e.Status.LastError))
                StatusChanged?.Invoke(this, $"{e.Status.DriverName} {e.Status.State}: {e.Status.LastError}");
            else
                StatusChanged?.Invoke(this, $"{e.Status.DriverName} {e.Status.State}.");
        }

        private void OnBlockForInputEvents(object sender, CommunicationBlockUpdatedEventArgs e)
        {
            ProcessInputEvents(e);
        }

        private void ProcessInputEvents(CommunicationBlockUpdatedEventArgs e)
        {
            if (_config == null) return;
            string key = $"{e.DeviceId}|{e.BlockId}";
            _previousValues.TryGetValue(key, out var previous);

            foreach (var input in _config.InputEvents.Where(i =>
                i.Source?.SourceKind == CommunicationInputSourceKind.CommunicationBlock
                && i.Source.DeviceId == e.DeviceId
                && i.Source.BlockId == e.BlockId))
            {
                if (!_inputEvaluator.Matches(input, e.Data, previous))
                    continue;

                InputEventTriggered?.Invoke(this, input);
                foreach (var heartbeat in _config.Heartbeats.Where(h => IsHeartbeatBoundToInput(h, input)))
                    ExecuteOutputEventAsync(heartbeat.OutputEventId, heartbeat.VariableValues).GetAwaiter().GetResult();
            }

            _previousValues[key] = e.Data != null ? (byte[])e.Data.Clone() : new byte[0];
        }

        private static bool IsHeartbeatBoundToInput(CommunicationHeartbeatConfig heartbeat, CommunicationInputEventConfig input)
        {
            if (heartbeat == null || input == null || !heartbeat.IsEnabled)
                return false;

            if (!string.Equals(heartbeat.InputEventId, input.EventId, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private void OnDeviceAdded(object sender, CommunicationDeviceConfig e) => CreateOrUpdateRuntime(e);
        private void OnDeviceUpdated(object sender, CommunicationDeviceConfig e) => CreateOrUpdateRuntime(e);
        private void OnDeviceRemoved(object sender, string e) => RemoveRuntime(e);
        private void OnConfigReset(object sender, EventArgs e) => SyncFromConfig();

        public void Dispose()
        {
            StopAllAsync().GetAwaiter().GetResult();
            foreach (var runtime in _runtimes.Values)
            {
                runtime.BlockUpdated -= OnBlockForInputEvents;
                runtime.StatusChanged -= OnRuntimeStatusChanged;
                runtime.Dispose();
            }
            _runtimes.Clear();
        }
    }
}
