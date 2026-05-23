using System;
using System.Globalization;
using System.Linq;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Core
{
    public sealed class CommunicationInputEvaluator
    {
        public bool Matches(CommunicationInputEventConfig config, byte[] current, byte[] previous)
        {
            if (config == null || current == null) return false;

            if (config.LengthCheckEnabled)
            {
                int charSize = config.TreatAsAscii ? 2 : 1;
                if (config.MinLengthEnabled && config.MinimumLength > 0
                    && current.Length < config.MinimumLength * charSize)
                    return false;
                if (config.ExactLengthEnabled && config.ExactLength > 0
                    && current.Length != config.ExactLength * charSize)
                    return false;
            }

            if (config.Rules == null || config.Rules.Count == 0) return true;

            foreach (var rule in config.Rules.OrderBy(r => r.Order))
            {
                if (!MatchesRule(rule, current, previous))
                    return false;
            }
            return true;
        }

        private static bool MatchesRule(CommunicationInputMatchRule rule, byte[] current, byte[] previous)
        {
            var currentSlice = Slice(current, rule.StartIndex, rule.Length);
            var previousSlice = Slice(previous, rule.StartIndex, rule.Length);

            if (rule.Operator == CommunicationMatchOperator.LengthAtLeast)
                return current?.Length >= ParseDouble(rule.MatchValue);

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
}
