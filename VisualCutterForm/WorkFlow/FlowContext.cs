using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using VisualMaster.Api;
using VisualMaster.WorkFlow.Data;
using CameraFrameSnapshot = VisualMaster.CameraLink.Api.CameraFrameSnapshot;

namespace VisualMaster.WorkFlow
{
    public class FlowContext : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, object> _pinValues = new ConcurrentDictionary<Guid, object>();
        private readonly ConcurrentDictionary<string, object> _variables = new ConcurrentDictionary<string, object>();
        private readonly List<FlowLogEntry> _logs = new List<FlowLogEntry>();
        private readonly Dictionary<string, CameraFrameSnapshot> _snapshots = new Dictionary<string, CameraFrameSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly string _subGraphId;

        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;
        public event Action<string> OnLogError;

        // ── 五类上下文入口 ───────────────────────────────────────────
        /// <summary>本次触发载荷。</summary>
        public FlowTriggerContext Trigger { get; private set; }

        /// <summary>本次运行元数据（RunId、子图名称、开始时间等）。</summary>
        public FlowRunMetadata RunMetadata { get; private set; }

        /// <summary>强类型服务访问入口（等同于 GetVariable&lt;IFlowServiceProvider&gt;("VisionController")）。</summary>
        public IFlowServiceProvider Services { get; private set; }

        /// <summary>本次运行中节点写入的结构化日志列表（只读视图）。</summary>
        public IReadOnlyList<FlowLogEntry> Logs => _logs;

        /// <summary>本次运行中相机节点捕获并注册的快照，按设备 ID 索引。</summary>
        public IReadOnlyDictionary<string, CameraFrameSnapshot> Snapshots => _snapshots;
        // ──────────────────────────────────────────────────────────────

        public FlowContext(string subGraphId = null)
        {
            _subGraphId = subGraphId ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 在执行开始前初始化运行元数据和服务引用。由 FlowExecutor 调用。
        /// </summary>
        public void Initialize(FlowRunMetadata meta, IFlowServiceProvider services)
        {
            RunMetadata = meta;
            Services = services;
            // 保持向后兼容：节点仍可通过 GetVariable 获取服务
            if (services != null)
                _variables["VisionController"] = services;
        }

        public void Log(string message, string nodeName = null)
        {
            _logs.Add(new FlowLogEntry { Level = FlowLogLevel.Info, Message = message, NodeName = nodeName });
            OnLog?.Invoke(message);
        }

        public void LogWarning(string message, string nodeName = null)
        {
            _logs.Add(new FlowLogEntry { Level = FlowLogLevel.Warning, Message = message, NodeName = nodeName });
            OnLogWarning?.Invoke(message);
        }

        public void LogError(string message, string nodeName = null)
        {
            _logs.Add(new FlowLogEntry { Level = FlowLogLevel.Error, Message = message, NodeName = nodeName });
            OnLogError?.Invoke(message);
        }

        /// <summary>
        /// 注册本次运行捕获的相机快照（AddRef 后由 context 管理生命周期）。
        /// 后续可通过 <see cref="Snapshots"/> 按设备 ID 获取。
        /// </summary>
        public void RegisterSnapshot(string deviceId, CameraFrameSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(deviceId) || snapshot == null) return;
            // 替换旧的同 key 快照
            if (_snapshots.TryGetValue(deviceId, out var old))
                old.Dispose();
            _snapshots[deviceId] = snapshot.AddRef();
        }

        public void SetPinValue(OutputPin pin, object value)
        {
            if (pin == null) return;
            _pinValues[pin.Id] = value;
        }

        public object GetPinValue(OutputPin pin)
        {
            if (pin == null) return null;
            _pinValues.TryGetValue(pin.Id, out var val);
            return val;
        }

        public bool TryGetPinValue(InputPin pin, out object value)
        {
            value = null;
            if (pin?.Source == null) return false;
            return _pinValues.TryGetValue(pin.Source.Id, out value);
        }

        public void SetVariable(string key, object value)
        {
            _variables[key] = value;
        }

        public void SetTrigger(FlowTriggerContext trigger)
        {
            Trigger?.Dispose();
            Trigger = trigger;

            if (trigger == null)
                return;

            _variables["FlowTrigger"] = trigger;
            _variables["FlowTrigger.CorrelationId"] = trigger.CorrelationId;
            _variables["FlowTrigger.SourceType"] = trigger.SourceType;
            _variables["FlowTrigger.SourceDeviceId"] = trigger.SourceDeviceId;
            _variables["FlowTrigger.SourceSlotId"] = trigger.SourceSlotId;
            _variables["FlowTrigger.CameraSnapshotId"] = trigger.CameraSnapshotId;
            _variables["FlowTrigger.CameraSnapshotSequence"] = trigger.CameraSnapshotSequence;
            _variables["FlowTrigger.SerialText"] = trigger.SerialText;
            _variables["FlowTrigger.SerialData"] = trigger.SerialData;
            _variables["FlowTrigger.MatchedRuleId"] = trigger.MatchedRuleId;
        }

        public bool HasTriggeredFrame(string slotId = null)
        {
            return TryGetTriggeredFrameClone(out _, slotId);
        }

        public bool TryGetTriggeredFrameClone(out Bitmap frame, string slotId = null)
        {
            frame = null;
            if (Trigger?.CameraSnapshot == null)
                return false;
            if (!string.IsNullOrEmpty(slotId) && !string.Equals(Trigger.SourceDeviceId, slotId, StringComparison.OrdinalIgnoreCase))
                return false;

            frame = Trigger.CameraSnapshot.CloneFrame();
            return true;
        }

        public bool TryGetTriggeredSerialPayload(out string text, out byte[] data, out string matchedRuleId, string slotId = null)
        {
            text = null;
            data = null;
            matchedRuleId = null;

            if (Trigger == null)
                return false;
            if (!string.IsNullOrEmpty(slotId) && !string.Equals(Trigger.SourceDeviceId, slotId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (Trigger.SerialText == null && Trigger.SerialData == null)
                return false;

            text = Trigger.SerialText;
            data = Trigger.SerialData != null ? (byte[])Trigger.SerialData.Clone() : null;
            matchedRuleId = Trigger.MatchedRuleId;
            return true;
        }

        public T GetVariable<T>(string key, T defaultValue = default(T))
        {
            if (_variables.TryGetValue(key, out var val) && val is T t)
                return t;
            return defaultValue;
        }

        public void Clear()
        {
            _pinValues.Clear();
            _variables.Clear();
            _logs.Clear();

            Trigger?.Dispose();
            Trigger = null;

            foreach (var snap in _snapshots.Values)
            {
                try { snap.Dispose(); } catch { }
            }
            _snapshots.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
