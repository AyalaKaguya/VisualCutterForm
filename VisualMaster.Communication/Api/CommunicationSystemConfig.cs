using System;
using System.Collections.Generic;
using System.Linq;
using VisualMaster.Config.Abstractions;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationSystemConfig : IConfigSection, ICloneable
    {
        private readonly List<CommunicationDeviceConfig> _devices = new List<CommunicationDeviceConfig>();
        private readonly List<CommunicationInputEventConfig> _inputEvents = new List<CommunicationInputEventConfig>();
        private readonly List<CommunicationOutputEventConfig> _outputEvents = new List<CommunicationOutputEventConfig>();
        private readonly List<CommunicationHeartbeatConfig> _heartbeats = new List<CommunicationHeartbeatConfig>();

        public string SectionKey => "communication";
        public int Version => 1;

        public IReadOnlyList<CommunicationDeviceConfig> Devices => _devices.AsReadOnly();
        public IReadOnlyList<CommunicationInputEventConfig> InputEvents => _inputEvents.AsReadOnly();
        public IReadOnlyList<CommunicationOutputEventConfig> OutputEvents => _outputEvents.AsReadOnly();
        public IReadOnlyList<CommunicationHeartbeatConfig> Heartbeats => _heartbeats.AsReadOnly();

        public event EventHandler<CommunicationDeviceConfig> DeviceAdded;
        public event EventHandler<string> DeviceRemoved;
        public event EventHandler<CommunicationDeviceConfig> DeviceUpdated;
        public event EventHandler EventsUpdated;
        public event EventHandler Reset;

        public void LoadFrom(IEnumerable<CommunicationDeviceConfig> devices)
        {
            _devices.Clear();
            if (devices != null)
            {
                foreach (var device in devices)
                    _devices.Add(device.Clone());
            }
            Reset?.Invoke(this, EventArgs.Empty);
        }

        public CommunicationDeviceConfig AddDevice(string driverName, string displayName = null)
        {
            var device = new CommunicationDeviceConfig
            {
                DriverName = driverName,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? driverName : displayName,
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

        public object Clone()
        {
            var clone = new CommunicationSystemConfig();
            clone._devices.AddRange(_devices.Select(d => d.Clone()));
            clone._inputEvents.AddRange(_inputEvents.Select(e => e.Clone()));
            clone._outputEvents.AddRange(_outputEvents.Select(e => e.Clone()));
            clone._heartbeats.AddRange(_heartbeats.Select(e => e.Clone()));
            return clone;
        }
    }
}
