using System;
using System.Collections.Generic;
using VisualCutterForm.Lib.Flow.Data;

namespace VisualCutterForm.Lib.Flow
{
    public abstract class NodePin
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public Type DataType { get; set; }
        public FlowNode Owner { get; set; }
        public bool IsConnected { get; protected set; }
        public bool UserDefined { get; set; }
        public abstract bool IsInput { get; }
        public abstract bool IsOutput { get; }

        public string TypeDisplayName
        {
            get
            {
                if (DataType == null) return "object";
                if (DataType == typeof(string)) return "string";
                if (DataType == typeof(int)) return "int";
                if (DataType == typeof(long)) return "long";
                if (DataType == typeof(float)) return "float";
                if (DataType == typeof(double)) return "double";
                if (DataType == typeof(bool)) return "bool";
                if (DataType == typeof(byte[])) return "byte[]";
                if (DataType == typeof(OpenCvSharp.Mat)) return "Mat";
                if (DataType == typeof(OpenCvSharp.Point2d)) return "Point2d";
                if (DataType == typeof(OpenCvSharp.Point2f)) return "Point2f";
                if (DataType == typeof(System.Drawing.Bitmap)) return "Bitmap";
                if (typeof(AcquisitionResult).IsAssignableFrom(DataType)) return "AcqResult";
                return DataType.Name;
            }
        }
    }

    public class InputPin : NodePin
    {
        public OutputPin Source { get; private set; }
        public bool Required { get; set; }
        public object DefaultValue { get; set; }
        public override bool IsInput => true;
        public override bool IsOutput => false;

        public InputPin(string name, Type dataType, FlowNode owner, bool required = false, bool userDefined = true, object defaultValue = null)
        {
            Name = name;
            DataType = dataType;
            Owner = owner;
            Required = required;
            UserDefined = userDefined;
            DefaultValue = defaultValue;
        }

        public void Connect(OutputPin source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (Source != null)
                Disconnect();

            if (!DataType.IsAssignableFrom(source.DataType))
                throw new InvalidOperationException(
                    $"Type mismatch: cannot connect {source.DataType.Name} to {DataType.Name}");

            Source = source;
            IsConnected = true;
            source.Targets.Add(this);
        }

        public void Disconnect()
        {
            if (Source != null)
            {
                Source.Targets.Remove(this);
                Source = null;
                IsConnected = false;
            }
        }

        public object GetValue(FlowContext context)
        {
            if (context == null) return null;

            if (Source != null)
                return context.GetPinValue(Source);

            if (context.TryGetPinValue(this, out var val))
                return val;

            return DefaultValue;
        }
    }

    public class OutputPin : NodePin
    {
        public List<InputPin> Targets { get; } = new List<InputPin>();
        public override bool IsInput => false;
        public override bool IsOutput => true;

        public OutputPin(string name, Type dataType, FlowNode owner, bool userDefined = true)
        {
            Name = name;
            DataType = dataType;
            Owner = owner;
            UserDefined = userDefined;
            IsConnected = false;
        }

        public void SetValue(FlowContext context, object value)
        {
            context.SetPinValue(this, value);
        }

        public void PropagateChanged()
        {
            IsConnected = Targets.Count > 0;
        }
    }
}
