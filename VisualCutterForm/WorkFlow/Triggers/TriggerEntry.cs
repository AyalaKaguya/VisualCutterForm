using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.WorkFlow.Triggers
{
    public enum TriggerSourceType
    {
        Manual,
        CameraFrame,
        Timer,
        SerialMatch,
    }

    public class TriggerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "新触发器";
        public TriggerSourceType SourceType { get; set; } = TriggerSourceType.Manual;
        public List<Guid> TargetSubGraphIds { get; set; } = new List<Guid>();
        public bool Enabled { get; set; } = true;

        public string CameraDeviceId { get; set; } = "";
        public int MaxConcurrent { get; set; } = 1;

        public int TimerIntervalMs { get; set; } = 100;

        public string SerialDeviceId { get; set; } = "";
        public Data.SerialTriggerRule MatchRule { get; set; }

        public string CameraSlotId
        {
            get { return CameraDeviceId; }
            set { CameraDeviceId = value ?? ""; }
        }

        public string SerialSlotId
        {
            get { return SerialDeviceId; }
            set { SerialDeviceId = value ?? ""; }
        }

        public Guid TargetSubGraphId
        {
            get { return TargetSubGraphIds != null && TargetSubGraphIds.Count > 0 ? TargetSubGraphIds[0] : Guid.Empty; }
            set
            {
                if (value == Guid.Empty)
                {
                    TargetSubGraphIds = new List<Guid>();
                    return;
                }

                if (TargetSubGraphIds == null)
                    TargetSubGraphIds = new List<Guid>();

                TargetSubGraphIds.Clear();
                TargetSubGraphIds.Add(value);
            }
        }

        public IReadOnlyList<Guid> GetTargetSubGraphIds()
        {
            if (TargetSubGraphIds == null)
                TargetSubGraphIds = new List<Guid>();

            return TargetSubGraphIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }

        public TriggerEntry Clone()
        {
            return new TriggerEntry
            {
                Id = Id,
                Name = Name,
                SourceType = SourceType,
                TargetSubGraphIds = GetTargetSubGraphIds().ToList(),
                Enabled = Enabled,
                CameraDeviceId = CameraDeviceId,
                MaxConcurrent = MaxConcurrent,
                TimerIntervalMs = TimerIntervalMs,
                SerialDeviceId = SerialDeviceId,
                MatchRule = MatchRule?.Clone(),
            };
        }
    }
}
