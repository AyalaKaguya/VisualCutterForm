using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationInputEventConfig
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string BlockId { get; set; }
        public bool LengthCheckEnabled { get; set; }
        public bool MinLengthEnabled { get; set; }
        public int MinimumLength { get; set; }
        public bool ExactLengthEnabled { get; set; }
        public int ExactLength { get; set; }
        public bool TreatAsAscii { get; set; }
        public List<CommunicationInputMatchRule> Rules { get; set; } = new List<CommunicationInputMatchRule>();

        public CommunicationInputEventConfig Clone()
        {
            return new CommunicationInputEventConfig
            {
                EventId = EventId,
                Name = Name,
                DeviceId = DeviceId,
                BlockId = BlockId,
                LengthCheckEnabled = LengthCheckEnabled,
                MinLengthEnabled = MinLengthEnabled,
                MinimumLength = MinimumLength,
                ExactLengthEnabled = ExactLengthEnabled,
                ExactLength = ExactLength,
                TreatAsAscii = TreatAsAscii,
                Rules = Rules?.Select(r => r.Clone()).ToList() ?? new List<CommunicationInputMatchRule>(),
            };
        }
    }

    public sealed class CommunicationInputMatchRule
    {
        public int Order { get; set; }
        public string TriggerName { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; } = 1;
        public CommunicationBlockDataType DataType { get; set; } = CommunicationBlockDataType.Bytes;
        public CommunicationByteOrder ByteOrder { get; set; } = CommunicationByteOrder.BigEndian;
        public CommunicationMatchOperator Operator { get; set; } = CommunicationMatchOperator.Equals;
        public string MatchValue { get; set; }

        public CommunicationInputMatchRule Clone()
        {
            return (CommunicationInputMatchRule)MemberwiseClone();
        }
    }

    public sealed class CommunicationOutputEventConfig
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string BlockId { get; set; }
        public List<CommunicationOutputVariable> Variables { get; set; } = new List<CommunicationOutputVariable>();
        public List<CommunicationOutputSegment> Segments { get; set; } = new List<CommunicationOutputSegment>();

        public CommunicationOutputEventConfig Clone()
        {
            return new CommunicationOutputEventConfig
            {
                EventId = EventId,
                Name = Name,
                DeviceId = DeviceId,
                BlockId = BlockId,
                Variables = Variables?.Select(v => v.Clone()).ToList() ?? new List<CommunicationOutputVariable>(),
                Segments = Segments?.Select(s => s.Clone()).ToList() ?? new List<CommunicationOutputSegment>(),
            };
        }
    }

    public sealed class CommunicationOutputVariable
    {
        public string Name { get; set; }
        public CommunicationBlockDataType DataType { get; set; } = CommunicationBlockDataType.Bytes;
        public CommunicationByteOrder ByteOrder { get; set; } = CommunicationByteOrder.BigEndian;

        public CommunicationOutputVariable Clone() => (CommunicationOutputVariable)MemberwiseClone();
    }

    public sealed class CommunicationOutputSegment
    {
        public CommunicationOutputSegmentKind Kind { get; set; } = CommunicationOutputSegmentKind.Constant;
        public string Value { get; set; }
        public CommunicationBlockDataType DataType { get; set; } = CommunicationBlockDataType.Bytes;
        public CommunicationByteOrder ByteOrder { get; set; } = CommunicationByteOrder.BigEndian;

        public CommunicationOutputSegment Clone() => (CommunicationOutputSegment)MemberwiseClone();
    }

    public sealed class CommunicationHeartbeatConfig
    {
        public string HeartbeatId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string InputEventId { get; set; }
        public string OutputEventId { get; set; }
        public bool IsEnabled { get; set; } = true;

        public CommunicationHeartbeatConfig Clone() => (CommunicationHeartbeatConfig)MemberwiseClone();
    }
}
