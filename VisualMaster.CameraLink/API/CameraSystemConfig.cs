using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.CameraLink.Api
{
    public sealed class CameraSystemConfig
    {
        private readonly List<CameraDeviceConfig> _devices = new List<CameraDeviceConfig>();
        private List<CameraDeviceConfig> _snapshot;

        public IReadOnlyList<CameraDeviceConfig> Devices => _devices.AsReadOnly();

        public event EventHandler<CameraDeviceConfig> DeviceAdded;
        public event EventHandler<string> DeviceRemoved;
        public event EventHandler<CameraDeviceConfig> DeviceUpdated;
        public event EventHandler Reset;
        public event EventHandler SaveRequested;

        public void LoadFrom(IEnumerable<CameraDeviceConfig> configs)
        {
            _devices.Clear();
            if (configs != null)
                foreach (var c in configs)
                    _devices.Add(c.Clone());
            TakeSnapshot();
        }

        public CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null)
        {
            var config = new CameraDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString(),
                DisplayName = displayName ?? $"相机 {_devices.Count + 1}",
                Settings = settings?.Clone() ?? new CameraSettings(),
            };
            _devices.Add(config);
            DeviceAdded?.Invoke(this, config);
            return config;
        }

        public void RemoveDevice(string deviceId)
        {
            var idx = _devices.FindIndex(d => d.DeviceId == deviceId);
            if (idx < 0) return;
            _devices.RemoveAt(idx);
            DeviceRemoved?.Invoke(this, deviceId);
        }

        public void UpdateDevice(CameraDeviceConfig updated)
        {
            if (updated == null) return;
            var idx = _devices.FindIndex(d => d.DeviceId == updated.DeviceId);
            if (idx < 0) return;
            _devices[idx] = updated.Clone();
            DeviceUpdated?.Invoke(this, _devices[idx]);
        }

        public CameraDeviceConfig GetDevice(string deviceId)
            => _devices.Find(d => d.DeviceId == deviceId)?.Clone();

        public void TakeSnapshot()
        {
            _snapshot = _devices.Select(d => d.Clone()).ToList();
        }

        public bool RevertChanges()
        {
            if (_snapshot == null) return false;
            _devices.Clear();
            foreach (var d in _snapshot)
                _devices.Add(d.Clone());
            Reset?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void RequestSave()
        {
            TakeSnapshot();
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
