using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationSystemConfig
    {
        private readonly List<CommunicationDeviceConfig> _devices = new List<CommunicationDeviceConfig>();
        private readonly List<CommunicationInputEventConfig> _inputEvents = new List<CommunicationInputEventConfig>();
        private readonly List<CommunicationOutputEventConfig> _outputEvents = new List<CommunicationOutputEventConfig>();
        private readonly List<CommunicationHeartbeatConfig> _heartbeats = new List<CommunicationHeartbeatConfig>();
        private List<CommunicationDeviceConfig> _snapshot;
        private List<CommunicationInputEventConfig> _inputSnapshot;
        private List<CommunicationOutputEventConfig> _outputSnapshot;
        private List<CommunicationHeartbeatConfig> _heartbeatSnapshot;

        public IReadOnlyList<CommunicationDeviceConfig> Devices => _devices.AsReadOnly();
        public IReadOnlyList<CommunicationInputEventConfig> InputEvents => _inputEvents.AsReadOnly();
        public IReadOnlyList<CommunicationOutputEventConfig> OutputEvents => _outputEvents.AsReadOnly();
        public IReadOnlyList<CommunicationHeartbeatConfig> Heartbeats => _heartbeats.AsReadOnly();

        public event EventHandler<CommunicationDeviceConfig> DeviceAdded;
        public event EventHandler<string> DeviceRemoved;
        public event EventHandler<CommunicationDeviceConfig> DeviceUpdated;
        public event EventHandler EventsUpdated;
        public event EventHandler Reset;
        public event EventHandler SaveRequested;

        public void LoadFrom(IEnumerable<CommunicationDeviceConfig> devices)
        {
            _devices.Clear();
            if (devices != null)
            {
                foreach (var device in devices)
                    _devices.Add(device.Clone());
            }
            TakeSnapshot();
            Reset?.Invoke(this, EventArgs.Empty);
        }

        public CommunicationDeviceConfig AddDevice(string driverName, string interfaceName, string displayName = null)
        {
            var device = new CommunicationDeviceConfig
            {
                DriverName = driverName,
                InterfaceName = interfaceName,
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? $"{driverName}-{interfaceName}"
                    : displayName,
            };
            _devices.Add(device);
            DeviceAdded?.Invoke(this, device.Clone());
            return device.Clone();
        }

        public void UpdateDevice(CommunicationDeviceConfig updated)
        {
            if (updated == null || string.IsNullOrEmpty(updated.DeviceId)) return;
            var index = _devices.FindIndex(d => d.DeviceId == updated.DeviceId);
            if (index < 0) return;
            _devices[index] = updated.Clone();
            DeviceUpdated?.Invoke(this, _devices[index].Clone());
        }

        public void RemoveDevice(string deviceId)
        {
            var index = _devices.FindIndex(d => d.DeviceId == deviceId);
            if (index < 0) return;
            _devices.RemoveAt(index);
            DeviceRemoved?.Invoke(this, deviceId);
        }

        public CommunicationDeviceConfig GetDevice(string deviceId)
        {
            return _devices.FirstOrDefault(d => d.DeviceId == deviceId)?.Clone();
        }

        public void TakeSnapshot()
        {
            _snapshot = _devices.Select(d => d.Clone()).ToList();
            _inputSnapshot = _inputEvents.Select(e => e.Clone()).ToList();
            _outputSnapshot = _outputEvents.Select(e => e.Clone()).ToList();
            _heartbeatSnapshot = _heartbeats.Select(e => e.Clone()).ToList();
        }

        public bool RevertChanges()
        {
            if (_snapshot == null) return false;
            _devices.Clear();
            _devices.AddRange(_snapshot.Select(d => d.Clone()));
            _inputEvents.Clear();
            _inputEvents.AddRange((_inputSnapshot ?? new List<CommunicationInputEventConfig>()).Select(e => e.Clone()));
            _outputEvents.Clear();
            _outputEvents.AddRange((_outputSnapshot ?? new List<CommunicationOutputEventConfig>()).Select(e => e.Clone()));
            _heartbeats.Clear();
            _heartbeats.AddRange((_heartbeatSnapshot ?? new List<CommunicationHeartbeatConfig>()).Select(e => e.Clone()));
            Reset?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void UpdateInputEvents(IEnumerable<CommunicationInputEventConfig> events)
        {
            _inputEvents.Clear();
            if (events != null)
                _inputEvents.AddRange(events.Select(e => e.Clone()));
            EventsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateOutputEvents(IEnumerable<CommunicationOutputEventConfig> events)
        {
            _outputEvents.Clear();
            if (events != null)
                _outputEvents.AddRange(events.Select(e => e.Clone()));
            EventsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateHeartbeats(IEnumerable<CommunicationHeartbeatConfig> heartbeats)
        {
            _heartbeats.Clear();
            if (heartbeats != null)
                _heartbeats.AddRange(heartbeats.Select(e => e.Clone()));
            EventsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void RequestSave()
        {
            TakeSnapshot();
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
