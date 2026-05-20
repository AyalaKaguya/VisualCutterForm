using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.Api
{
    /// <summary>
    /// 相机模块的配置容器，由上层调用方创建并注入到 ICameraManager。
    /// 支持即时生效的配置修改、快照回滚，以及将保存请求委托给上层实现。
    /// </summary>
    public sealed class CameraSystemConfig
    {
        private readonly List<CameraDeviceConfig> _devices = new List<CameraDeviceConfig>();
        private List<CameraDeviceConfig> _snapshot;

        // ── 只读视图 ──────────────────────────────────────────────────
        public IReadOnlyList<CameraDeviceConfig> Devices => _devices.AsReadOnly();

        // ── 变更通知事件 ──────────────────────────────────────────────
        public event EventHandler<CameraDeviceConfig> DeviceAdded;
        public event EventHandler<string> DeviceRemoved;
        public event EventHandler<CameraDeviceConfig> DeviceUpdated;

        /// <summary>由上层模块赋值；调用 RequestSave() 时触发，上层负责真正持久化。</summary>
        public event EventHandler SaveRequested;

        // ── 初始化 ────────────────────────────────────────────────────
        /// <summary>从已持久化的配置列表初始化（例如从 JSON 加载后调用）。</summary>
        public void LoadFrom(IEnumerable<CameraDeviceConfig> configs)
        {
            _devices.Clear();
            if (configs != null)
                foreach (var c in configs)
                    _devices.Add(c.Clone());
            TakeSnapshot();
        }

        // ── CRUD ──────────────────────────────────────────────────────
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

        /// <summary>更新设备配置（立即生效）。</summary>
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

        // ── 快照与回滚 ────────────────────────────────────────────────
        /// <summary>创建当前配置的快照，可通过 RevertChanges() 恢复。</summary>
        public void TakeSnapshot()
        {
            _snapshot = _devices.Select(d => d.Clone()).ToList();
        }

        /// <summary>将配置恢复到上次快照的状态。返回 true 表示有快照可恢复。</summary>
        public bool RevertChanges()
        {
            if (_snapshot == null) return false;
            _devices.Clear();
            foreach (var d in _snapshot)
                _devices.Add(d.Clone());
            return true;
        }

        /// <summary>通知上层模块持久化当前配置（上层通过 SaveRequested 事件监听）。</summary>
        public void RequestSave()
        {
            TakeSnapshot();
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
