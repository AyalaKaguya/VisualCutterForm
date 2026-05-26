using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationInputEventConfig
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public CommunicationInputSourceConfig Source { get; set; } = new CommunicationInputSourceConfig();
        public CommunicationInputPayloadConfig Payload { get; set; } = new CommunicationInputPayloadConfig();
        public CommunicationInputMatchMode MatchMode { get; set; } = CommunicationInputMatchMode.AllConditions;
        public string DeviceId
        {
            get => Source?.DeviceId;
            set => EnsureSource().DeviceId = value;
        }
        public string BlockId
        {
            get => Source?.BlockId;
            set => EnsureSource().BlockId = value;
        }
        public bool LengthCheckEnabled { get; set; }
        public bool MinLengthEnabled { get; set; }
        public int MinimumLength { get; set; }
        public bool ExactLengthEnabled { get; set; }
        public int ExactLength { get; set; }
        public bool TreatAsAscii { get; set; }
        public List<CommunicationInputMatchRule> Rules { get; set; } = new List<CommunicationInputMatchRule>();
        public List<CommunicationInputConditionConfig> Conditions { get; set; } = new List<CommunicationInputConditionConfig>();

        public CommunicationInputEventConfig Clone()
        {
            return new CommunicationInputEventConfig
            {
                EventId = EventId,
                Name = Name,
                Source = Source?.Clone() ?? new CommunicationInputSourceConfig(),
                Payload = Payload?.Clone() ?? CreatePayloadFromLegacySource(Source),
                MatchMode = MatchMode,
                LengthCheckEnabled = LengthCheckEnabled,
                MinLengthEnabled = MinLengthEnabled,
                MinimumLength = MinimumLength,
                ExactLengthEnabled = ExactLengthEnabled,
                ExactLength = ExactLength,
                TreatAsAscii = TreatAsAscii,
                Rules = Rules?.Select(r => r.Clone()).ToList() ?? new List<CommunicationInputMatchRule>(),
                Conditions = Conditions?.Select(r => r.Clone()).ToList() ?? new List<CommunicationInputConditionConfig>(),
            };
        }

        private CommunicationInputSourceConfig EnsureSource()
        {
            return Source ?? (Source = new CommunicationInputSourceConfig());
        }

        private static CommunicationInputPayloadConfig CreatePayloadFromLegacySource(CommunicationInputSourceConfig source)
        {
            return new CommunicationInputPayloadConfig
            {
                PayloadKind = source?.PayloadKind ?? CommunicationInputPayloadKind.Bytes,
                EncodingName = string.IsNullOrWhiteSpace(source?.EncodingName) ? "UTF-8" : source.EncodingName,
            };
        }
    }

    public sealed class CommunicationInputSourceConfig
    {
        public CommunicationInputSourceKind SourceKind { get; set; } = CommunicationInputSourceKind.CommunicationBlock;
        public CommunicationInputPayloadKind PayloadKind { get; set; } = CommunicationInputPayloadKind.Bytes;
        public string DeviceId { get; set; }
        public string BlockId { get; set; }
        public string EncodingName { get; set; } = "UTF-8";

        public CommunicationInputSourceConfig Clone()
        {
            return new CommunicationInputSourceConfig
            {
                SourceKind = SourceKind,
                PayloadKind = PayloadKind,
                DeviceId = DeviceId,
                BlockId = BlockId,
                EncodingName = EncodingName,
            };
        }
    }

    public sealed class CommunicationInputPayloadConfig
    {
        public CommunicationInputPayloadKind PayloadKind { get; set; } = CommunicationInputPayloadKind.Bytes;
        public string EncodingName { get; set; } = "UTF-8";
        public string JsonPathMode { get; set; }

        public CommunicationInputPayloadConfig Clone()
        {
            return new CommunicationInputPayloadConfig
            {
                PayloadKind = PayloadKind,
                EncodingName = EncodingName,
                JsonPathMode = JsonPathMode,
            };
        }
    }

    public sealed class CommunicationInputConditionConfig
    {
        public string ConditionId { get; set; } = Guid.NewGuid().ToString("N");
        public int Order { get; set; }
        public string Name { get; set; }
        public string TargetPath { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; } = 1;
        public CommunicationBlockDataType ValueType { get; set; } = CommunicationBlockDataType.Bytes;
        public CommunicationByteOrder ByteOrder { get; set; } = CommunicationByteOrder.BigEndian;
        public CommunicationMatchOperator Operator { get; set; } = CommunicationMatchOperator.Equals;
        public string ExpectedValue { get; set; }
        public string BeforeValue { get; set; }
        public string AfterValue { get; set; }

        public CommunicationInputConditionConfig Clone()
        {
            return new CommunicationInputConditionConfig
            {
                ConditionId = ConditionId,
                Order = Order,
                Name = Name,
                TargetPath = TargetPath,
                StartIndex = StartIndex,
                Length = Length,
                ValueType = ValueType,
                ByteOrder = ByteOrder,
                Operator = Operator,
                ExpectedValue = ExpectedValue,
                BeforeValue = BeforeValue,
                AfterValue = AfterValue,
            };
        }

        public static CommunicationInputConditionConfig FromRule(CommunicationInputMatchRule rule)
        {
            if (rule == null) return null;
            return new CommunicationInputConditionConfig
            {
                ConditionId = string.IsNullOrWhiteSpace(rule.RuleId) ? Guid.NewGuid().ToString("N") : rule.RuleId,
                Order = rule.Order,
                Name = rule.TriggerName,
                StartIndex = rule.StartIndex,
                Length = rule.Length,
                ValueType = rule.DataType,
                ByteOrder = rule.ByteOrder,
                Operator = rule.Operator,
                ExpectedValue = rule.MatchValue,
                BeforeValue = rule.BeforeValue,
                AfterValue = rule.AfterValue,
            };
        }

        public CommunicationInputMatchRule ToRule()
        {
            return new CommunicationInputMatchRule
            {
                RuleId = ConditionId,
                Order = Order,
                TriggerName = Name,
                StartIndex = StartIndex,
                Length = Length,
                DataType = ValueType,
                ByteOrder = ByteOrder,
                Operator = Operator,
                MatchValue = ExpectedValue,
                BeforeValue = BeforeValue,
                AfterValue = AfterValue,
            };
        }
    }

    public sealed class CommunicationInputMatchRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString("N");
        public int Order { get; set; }
        public string TriggerName { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; } = 1;
        public CommunicationBlockDataType DataType { get; set; } = CommunicationBlockDataType.Bytes;
        public CommunicationByteOrder ByteOrder { get; set; } = CommunicationByteOrder.BigEndian;
        public CommunicationMatchOperator Operator { get; set; } = CommunicationMatchOperator.Equals;
        public string MatchValue { get; set; }
        public string BeforeValue { get; set; }
        public string AfterValue { get; set; }

        public CommunicationInputMatchRule Clone()
        {
            return new CommunicationInputMatchRule
            {
                RuleId = RuleId,
                Order = Order,
                TriggerName = TriggerName,
                StartIndex = StartIndex,
                Length = Length,
                DataType = DataType,
                ByteOrder = ByteOrder,
                Operator = Operator,
                MatchValue = MatchValue,
                BeforeValue = BeforeValue,
                AfterValue = AfterValue,
            };
        }
    }

    public sealed class CommunicationOutputEventConfig
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string BlockId { get; set; }
        public CommunicationProtocolAssemblyConfig ProtocolAssembly { get; set; } = new CommunicationProtocolAssemblyConfig();
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
                ProtocolAssembly = ProtocolAssembly?.Clone() ?? new CommunicationProtocolAssemblyConfig(),
                Variables = Variables?.Select(v => v.Clone()).ToList() ?? new List<CommunicationOutputVariable>(),
                Segments = Segments?.Select(s => s.Clone()).ToList() ?? new List<CommunicationOutputSegment>(),
            };
        }
    }

    public sealed class CommunicationProtocolAssemblyConfig
    {
        public string HeaderHex { get; set; }
        public bool CrcEnabled { get; set; }
        public CommunicationCrcMethod CrcMethod { get; set; } = CommunicationCrcMethod.None;
        public CommunicationByteOrder CrcByteOrder { get; set; } = CommunicationByteOrder.LittleEndian;
        public bool CrcIncludesHeader { get; set; } = true;

        public CommunicationProtocolAssemblyConfig Clone() => (CommunicationProtocolAssemblyConfig)MemberwiseClone();
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
        public string InputRuleId { get; set; }
        public string OutputEventId { get; set; }
        public Dictionary<string, string> VariableValues { get; set; } = new Dictionary<string, string>();
        public bool IsEnabled { get; set; } = true;

        public CommunicationHeartbeatConfig Clone()
        {
            return new CommunicationHeartbeatConfig
            {
                HeartbeatId = HeartbeatId,
                Name = Name,
                InputEventId = InputEventId,
                InputRuleId = InputRuleId,
                OutputEventId = OutputEventId,
                VariableValues = VariableValues != null
                    ? new Dictionary<string, string>(VariableValues)
                    : new Dictionary<string, string>(),
                IsEnabled = IsEnabled,
            };
        }
    }
}
