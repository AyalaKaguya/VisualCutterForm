using System;

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
        public Guid TargetSubGraphId { get; set; }
        public bool Enabled { get; set; } = true;

        public string CameraSlotId { get; set; } = "";
        public int MaxConcurrent { get; set; } = 1;

        public int TimerIntervalMs { get; set; } = 100;

        public string SerialSlotId { get; set; } = "";
        public Data.SerialTriggerRule MatchRule { get; set; }

        public TriggerEntry Clone()
        {
            return new TriggerEntry
            {
                Id = Id,
                Name = Name,
                SourceType = SourceType,
                TargetSubGraphId = TargetSubGraphId,
                Enabled = Enabled,
                CameraSlotId = CameraSlotId,
                MaxConcurrent = MaxConcurrent,
                TimerIntervalMs = TimerIntervalMs,
                SerialSlotId = SerialSlotId,
                MatchRule = MatchRule?.Clone(),
            };
        }
    }
}
