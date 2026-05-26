using System;

namespace VisualMaster.CameraLink.Api
{
    public enum RuntimeDiagnosticEventType
    {
        SnapshotPublished,
        TriggerDispatched,
        FlowStarted,
        FlowCompleted,
        FlowFailed,
    }

    public sealed class RuntimeDiagnosticEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public RuntimeDiagnosticEventType EventType { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.Now;
        public string CorrelationId { get; set; }
        public string DeviceId { get; set; }
        public string SnapshotId { get; set; }
        public long SnapshotSequence { get; set; }
        public string TriggerId { get; set; }
        public string TriggerName { get; set; }
        public string FlowId { get; set; }
        public string FlowName { get; set; }
        public string Message { get; set; }
    }
}
