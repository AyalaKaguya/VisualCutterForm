using System;
using System.Drawing;
using VisualMaster.Api;
using VisualMaster.WorkFlow.Triggers;
using CameraFrameSnapshot = VisualMaster.CameraLink.Api.CameraFrameSnapshot;

namespace VisualMaster.WorkFlow.Data
{
    public sealed class FlowTriggerContext : IDisposable
    {
        public Guid TriggerId { get; set; }
        public string TriggerName { get; set; }
        public TriggerSourceType SourceType { get; set; }
        public string SourceDeviceId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.Now;
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
        public CameraFrameSnapshot CameraSnapshot { get; set; }
        public string SerialText { get; set; }
        public byte[] SerialData { get; set; }
        public string MatchedRuleId { get; set; }
        public string CameraSnapshotId => CameraSnapshot?.SnapshotId ?? string.Empty;
        public long CameraSnapshotSequence => CameraSnapshot?.SequenceNumber ?? 0;
        public Bitmap CameraFrame => CameraSnapshot?.Frame;

        public string SourceSlotId
        {
            get { return SourceDeviceId; }
            set { SourceDeviceId = value; }
        }

        public FlowTriggerContext Clone()
        {
            return new FlowTriggerContext
            {
                TriggerId = TriggerId,
                TriggerName = TriggerName,
                SourceType = SourceType,
                SourceDeviceId = SourceDeviceId,
                OccurredAt = OccurredAt,
                CorrelationId = CorrelationId,
                CameraSnapshot = CameraSnapshot?.AddRef(),
                SerialText = SerialText,
                SerialData = SerialData != null ? (byte[])SerialData.Clone() : null,
                MatchedRuleId = MatchedRuleId,
            };
        }

        public void Dispose()
        {
            CameraSnapshot?.Dispose();
            CameraSnapshot = null;
        }
    }
}