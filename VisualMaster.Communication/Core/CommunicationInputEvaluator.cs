using System;
using System.Globalization;
using System.Linq;
using System.Text;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Core
{
    public sealed class CommunicationInputEvaluator
    {
        public bool Matches(CommunicationInputEventConfig config, byte[] current, byte[] previous)
        {
            if (config == null || current == null) return false;
            var currentPayload = CommunicationInputPayload.FromBytes(config.Payload, config.Source, current);
            var previousPayload = CommunicationInputPayload.FromBytes(config.Payload, config.Source, previous);
            return Matches(config, currentPayload, previousPayload);
        }

        public bool Matches(
            CommunicationInputEventConfig config,
            CommunicationInputPayload current,
            CommunicationInputPayload previous)
        {
            if (config == null || current == null) return false;

            if (config.LengthCheckEnabled)
            {
                int length = current.Length;
                if (config.MinLengthEnabled && config.MinimumLength > 0
                    && length < config.MinimumLength)
                    return false;
                if (config.ExactLengthEnabled && config.ExactLength > 0
                    && length != config.ExactLength)
                    return false;
            }

            var conditions = GetConditions(config).ToList();
            if (conditions.Count == 0) return true;

            if (config.MatchMode == CommunicationInputMatchMode.AnyCondition)
            {
                foreach (var condition in conditions)
                {
                    if (MatchesCondition(condition, current, previous))
                        return true;
                }
                return false;
            }

            foreach (var condition in conditions)
            {
                if (!MatchesCondition(condition, current, previous))
                    return false;
            }

            return true;
        }

        private static IQueryable<CommunicationInputConditionConfig> GetConditions(CommunicationInputEventConfig config)
        {
            var conditions = config.Conditions != null && config.Conditions.Count > 0
                ? config.Conditions
                : config.Rules?.Select(CommunicationInputConditionConfig.FromRule).Where(c => c != null).ToList();

            return (conditions ?? Enumerable.Empty<CommunicationInputConditionConfig>())
                .OrderBy(r => r.Order)
                .AsQueryable();
        }

        private static bool MatchesCondition(CommunicationInputConditionConfig condition, CommunicationInputPayload current, CommunicationInputPayload previous)
        {
            return MatchesRule(condition.ToRule(), current, previous);
        }

        private static bool MatchesRule(CommunicationInputMatchRule rule, CommunicationInputPayload current, CommunicationInputPayload previous)
        {
            var currentSlice = Slice(current.RawBytes, rule.StartIndex, rule.Length);
            var previousSlice = Slice(previous?.RawBytes, rule.StartIndex, rule.Length);

            if (rule.Operator == CommunicationMatchOperator.LengthAtLeast)
                return current.Length >= ParseDouble(rule.MatchValue);

            if (current.PayloadKind != CommunicationInputPayloadKind.Bytes)
                return MatchesTextRule(rule, current, previous);

            if (rule.Operator == CommunicationMatchOperator.RisingEdge)
            {
                var cv = CommunicationDataConverter.Decode(currentSlice, rule.DataType, rule.ByteOrder);
                var pv = CommunicationDataConverter.Decode(previousSlice, rule.DataType, rule.ByteOrder);
                return Compare(pv, 0) == 0 && Compare(cv, 0) != 0;
            }

            if (rule.Operator == CommunicationMatchOperator.FallingEdge)
            {
                var cv = CommunicationDataConverter.Decode(currentSlice, rule.DataType, rule.ByteOrder);
                var pv = CommunicationDataConverter.Decode(previousSlice, rule.DataType, rule.ByteOrder);
                return Compare(pv, 0) != 0 && Compare(cv, 0) == 0;
            }

            var currentValue = CommunicationDataConverter.Decode(currentSlice, rule.DataType, rule.ByteOrder);
            var previousValue = CommunicationDataConverter.Decode(previousSlice, rule.DataType, rule.ByteOrder);
            var targetValue = ParseValue(rule.MatchValue, rule.DataType, rule.ByteOrder);

            switch (rule.Operator)
            {
                case CommunicationMatchOperator.Equals: return Compare(currentValue, targetValue) == 0;
                case CommunicationMatchOperator.GreaterThan: return Compare(currentValue, targetValue) > 0;
                case CommunicationMatchOperator.GreaterThanOrEqual: return Compare(currentValue, targetValue) >= 0;
                case CommunicationMatchOperator.LessThan: return Compare(currentValue, targetValue) < 0;
                case CommunicationMatchOperator.LessThanOrEqual: return Compare(currentValue, targetValue) <= 0;
                case CommunicationMatchOperator.ChangedTo:
                    if (!string.IsNullOrWhiteSpace(rule.BeforeValue) || !string.IsNullOrWhiteSpace(rule.AfterValue))
                    {
                        var beforeTarget = string.IsNullOrWhiteSpace(rule.BeforeValue) ? null
                            : ParseValue(rule.BeforeValue, rule.DataType, rule.ByteOrder);
                        var afterTarget = string.IsNullOrWhiteSpace(rule.AfterValue) ? targetValue
                            : ParseValue(rule.AfterValue, rule.DataType, rule.ByteOrder);
                        var beforeMatch = beforeTarget == null || Compare(previousValue, beforeTarget) == 0;
                        return beforeMatch && Compare(currentValue, afterTarget) == 0;
                    }
                    return Compare(previousValue, targetValue) != 0 && Compare(currentValue, targetValue) == 0;
                case CommunicationMatchOperator.ChangedFrom: return Compare(previousValue, targetValue) == 0 && Compare(currentValue, targetValue) != 0;
                case CommunicationMatchOperator.Contains:
                    return CommunicationDataConverter.ToHex(currentSlice).Contains(rule.MatchValue ?? "");
                default: return false;
            }
        }

        private static bool MatchesTextRule(
            CommunicationInputMatchRule rule,
            CommunicationInputPayload current,
            CommunicationInputPayload previous)
        {
            string currentText = current.Text ?? string.Empty;
            string previousText = previous?.Text ?? string.Empty;
            string target = rule.MatchValue ?? string.Empty;

            switch (rule.Operator)
            {
                case CommunicationMatchOperator.Equals:
                    return string.Equals(currentText, target, StringComparison.Ordinal);
                case CommunicationMatchOperator.Contains:
                    return currentText.Contains(target);
                case CommunicationMatchOperator.ChangedTo:
                    return !string.Equals(previousText, target, StringComparison.Ordinal)
                        && string.Equals(currentText, target, StringComparison.Ordinal);
                case CommunicationMatchOperator.ChangedFrom:
                    return string.Equals(previousText, target, StringComparison.Ordinal)
                        && !string.Equals(currentText, target, StringComparison.Ordinal);
                default:
                    return MatchesRule(rule, new CommunicationInputPayload(CommunicationInputPayloadKind.Bytes, current.RawBytes, null), new CommunicationInputPayload(CommunicationInputPayloadKind.Bytes, previous?.RawBytes, null));
            }
        }

        private static byte[] Slice(byte[] data, int start, int length)
        {
            if (data == null || start < 0 || length <= 0 || start >= data.Length) return new byte[0];
            return data.Skip(start).Take(length).ToArray();
        }

        private static object ParseValue(string value, CommunicationBlockDataType type, CommunicationByteOrder order)
        {
            if (type == CommunicationBlockDataType.Bytes)
                return CommunicationDataConverter.FromHex(value);
            return CommunicationDataConverter.Decode(CommunicationDataConverter.Encode(value, type, order), type, order);
        }

        private static int Compare(object left, object right)
        {
            if (left is byte[] lb && right is byte[] rb)
                return CommunicationDataConverter.ToHex(lb).CompareTo(CommunicationDataConverter.ToHex(rb));
            if (left is string || right is string)
                return string.Compare(Convert.ToString(left), Convert.ToString(right), StringComparison.Ordinal);
            return ParseDouble(Convert.ToString(left, CultureInfo.InvariantCulture))
                .CompareTo(ParseDouble(Convert.ToString(right, CultureInfo.InvariantCulture)));
        }

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        }
    }

    public sealed class CommunicationInputPayload
    {
        public CommunicationInputPayload(CommunicationInputPayloadKind payloadKind, byte[] rawBytes, string text)
        {
            PayloadKind = payloadKind;
            RawBytes = rawBytes != null ? (byte[])rawBytes.Clone() : new byte[0];
            Text = text;
        }

        public CommunicationInputPayloadKind PayloadKind { get; }
        public byte[] RawBytes { get; }
        public string Text { get; }
        public int Length => PayloadKind == CommunicationInputPayloadKind.Bytes
            ? RawBytes.Length
            : (Text ?? string.Empty).Length;

        public static CommunicationInputPayload FromBytes(
            CommunicationInputPayloadConfig payload,
            CommunicationInputSourceConfig source,
            byte[] data)
        {
            var kind = payload?.PayloadKind ?? source?.PayloadKind ?? CommunicationInputPayloadKind.Bytes;
            var bytes = data != null ? (byte[])data.Clone() : new byte[0];
            if (kind == CommunicationInputPayloadKind.Bytes)
                return new CommunicationInputPayload(kind, bytes, null);

            var encoding = ResolveEncoding(payload?.EncodingName ?? source?.EncodingName);
            return new CommunicationInputPayload(kind, bytes, encoding.GetString(bytes));
        }

        private static Encoding ResolveEncoding(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
                return Encoding.UTF8;

            try { return Encoding.GetEncoding(encodingName); }
            catch { return Encoding.UTF8; }
        }
    }
}
