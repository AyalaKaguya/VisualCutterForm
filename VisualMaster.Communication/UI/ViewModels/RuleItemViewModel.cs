using System;
using System.Collections.ObjectModel;
using System.Linq;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI.ViewModels
{
    public sealed class DataTypeEntry
    {
        public string Display { get; }
        public CommunicationBlockDataType Type { get; }
        public bool IsSingleByte { get; }

        public DataTypeEntry(string display, CommunicationBlockDataType type, bool isSingleByte = false)
        {
            Display = display;
            Type = type;
            IsSingleByte = isSingleByte;
        }

        public override string ToString() => Display;
    }

    public sealed class ByteOrderEntry
    {
        public string Display { get; }
        public CommunicationByteOrder Order { get; }

        public ByteOrderEntry(string display, CommunicationByteOrder order)
        {
            Display = display;
            Order = order;
        }

        public override string ToString() => Display;
    }

    public sealed class OperatorEntry
    {
        public string Display { get; }
        public CommunicationMatchOperator Operator { get; }

        public OperatorEntry(string display, CommunicationMatchOperator op)
        {
            Display = display;
            Operator = op;
        }

        public override string ToString() => Display;
    }

    public sealed class RuleItemViewModel : NotifyBase
    {
        private readonly CommunicationInputMatchRule _rule;
        private int _index;

        public CommunicationInputMatchRule Rule => _rule;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _rule.TriggerName ?? "";
            set { _rule.TriggerName = value; OnPropertyChanged(); FireChanged(); }
        }

        public int StartOffset
        {
            get => _rule.StartIndex;
            set
            {
                if (value < 0) value = 0;
                _rule.StartIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndOffset));
                if (_rule.Length < 1) _rule.Length = 1;
                FireChanged();
            }
        }

        public int EndOffset
        {
            get => _rule.StartIndex + _rule.Length;
            set
            {
                var newLen = value - _rule.StartIndex;
                if (newLen < 1) newLen = 1;
                _rule.Length = newLen;
                OnPropertyChanged();
                FireChanged();
            }
        }

        public CommunicationMatchOperator Operator
        {
            get => _rule.Operator;
            set
            {
                _rule.Operator = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChangedTo));
                OnPropertyChanged(nameof(NeedsOneParam));
                OnPropertyChanged(nameof(NeedsTwoParams));
                if (value == CommunicationMatchOperator.RisingEdge || value == CommunicationMatchOperator.FallingEdge)
                {
                    _rule.MatchValue = null;
                    _rule.BeforeValue = null;
                    _rule.AfterValue = null;
                    OnPropertyChanged(nameof(MatchValue));
                    OnPropertyChanged(nameof(BeforeValue));
                    OnPropertyChanged(nameof(AfterValue));
                }
                else if (value == CommunicationMatchOperator.ChangedTo)
                {
                    _rule.MatchValue = null;
                    OnPropertyChanged(nameof(MatchValue));
                }
                else
                {
                    _rule.BeforeValue = null;
                    _rule.AfterValue = null;
                    OnPropertyChanged(nameof(BeforeValue));
                    OnPropertyChanged(nameof(AfterValue));
                }
                FireChanged();
            }
        }

        public string MatchValue
        {
            get => _rule.MatchValue ?? "";
            set { _rule.MatchValue = value; OnPropertyChanged(); FireChanged(); }
        }

        public string BeforeValue
        {
            get => _rule.BeforeValue ?? "";
            set { _rule.BeforeValue = value; OnPropertyChanged(); FireChanged(); }
        }

        public string AfterValue
        {
            get => _rule.AfterValue ?? "";
            set { _rule.AfterValue = value; OnPropertyChanged(); FireChanged(); }
        }

        public DataTypeEntry SelectedType
        {
            get
            {
                if (_rule.DataType == CommunicationBlockDataType.Bytes && _rule.Length == 1)
                    return DataTypeOptions.FirstOrDefault(o => o.IsSingleByte) ?? DataTypeOptions.First(o => o.Type == CommunicationBlockDataType.Bytes);
                return DataTypeOptions.First(o => o.Type == _rule.DataType && !o.IsSingleByte);
            }
            set
            {
                if (value == null) return;
                _rule.DataType = value.Type;
                if (value.IsSingleByte) _rule.Length = 1;
                OnPropertyChanged();
                FireChanged();
            }
        }

        public ByteOrderEntry SelectedByteOrder
        {
            get => ByteOrderOptions.First(o => o.Order == _rule.ByteOrder);
            set
            {
                if (value == null) return;
                _rule.ByteOrder = value.Order;
                OnPropertyChanged();
                FireChanged();
            }
        }

        public OperatorEntry SelectedOperator
        {
            get
            {
                var found = OperatorOptions.FirstOrDefault(o => o.Operator == _rule.Operator);
                if (found != null) return found;
                _rule.Operator = CommunicationMatchOperator.Equals;
                return OperatorOptions.First(o => o.Operator == _rule.Operator);
            }
            set
            {
                if (value == null) return;
                Operator = value.Operator;
            }
        }

        public bool IsChangedTo => _rule.Operator == CommunicationMatchOperator.ChangedTo;
        public bool NeedsOneParam =>
            _rule.Operator == CommunicationMatchOperator.Equals ||
            _rule.Operator == CommunicationMatchOperator.GreaterThan ||
            _rule.Operator == CommunicationMatchOperator.GreaterThanOrEqual ||
            _rule.Operator == CommunicationMatchOperator.LessThan ||
            _rule.Operator == CommunicationMatchOperator.LessThanOrEqual;
        public bool NeedsTwoParams => _rule.Operator == CommunicationMatchOperator.ChangedTo;

        public static ObservableCollection<DataTypeEntry> DataTypeOptions { get; } = new ObservableCollection<DataTypeEntry>
        {
            new DataTypeEntry("字符串", CommunicationBlockDataType.AsciiString),
            new DataTypeEntry("int", CommunicationBlockDataType.Int32),
            new DataTypeEntry("short", CommunicationBlockDataType.Int16),
            new DataTypeEntry("byte", CommunicationBlockDataType.Bytes, isSingleByte: true),
            new DataTypeEntry("byte[]", CommunicationBlockDataType.Bytes),
            new DataTypeEntry("float", CommunicationBlockDataType.Single),
            new DataTypeEntry("double", CommunicationBlockDataType.Double),
        };

        public static ObservableCollection<ByteOrderEntry> ByteOrderOptions { get; } = new ObservableCollection<ByteOrderEntry>
        {
            new ByteOrderEntry("ABCD 大端", CommunicationByteOrder.BigEndian),
            new ByteOrderEntry("BADC 小端", CommunicationByteOrder.LittleEndian),
        };

        public static ObservableCollection<OperatorEntry> OperatorOptions { get; } = new ObservableCollection<OperatorEntry>
        {
            new OperatorEntry("上升沿", CommunicationMatchOperator.RisingEdge),
            new OperatorEntry("下降沿", CommunicationMatchOperator.FallingEdge),
            new OperatorEntry("变为", CommunicationMatchOperator.ChangedTo),
            new OperatorEntry("等于", CommunicationMatchOperator.Equals),
            new OperatorEntry("大于", CommunicationMatchOperator.GreaterThan),
            new OperatorEntry("小于", CommunicationMatchOperator.LessThan),
            new OperatorEntry("大于等于", CommunicationMatchOperator.GreaterThanOrEqual),
            new OperatorEntry("小于等于", CommunicationMatchOperator.LessThanOrEqual),
        };

        public event Action<RuleItemViewModel> Changed;
        public event Action<RuleItemViewModel> DeleteRequested;

        public RuleItemViewModel(CommunicationInputMatchRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public void FireChanged() => Changed?.Invoke(this);
        public void FireDelete() => DeleteRequested?.Invoke(this);
    }
}
