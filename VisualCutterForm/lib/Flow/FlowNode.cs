using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace VisualCutterForm.Lib.Flow
{
    public abstract class FlowNode
    {
        private static readonly ConcurrentDictionary<Type, NodeMetadata> _metadataCache
            = new ConcurrentDictionary<Type, NodeMetadata>();

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Category { get; set; }
        public List<InputPin> Inputs { get; } = new List<InputPin>();
        public List<OutputPin> Outputs { get; } = new List<OutputPin>();

        [Browsable(false)]
        [Newtonsoft.Json.JsonIgnore]
        public double NodeX { get; set; }

        [Browsable(false)]
        [Newtonsoft.Json.JsonIgnore]
        public double NodeY { get; set; }

        [Browsable(false)]
        [Newtonsoft.Json.JsonIgnore]
        public double LastExecutionTimeMs { get; set; }

        [Browsable(false)]
        public virtual bool IsBackgroundWorker
        {
            get
            {
                return GetMetadata().IsBackground;
            }
        }

        protected FlowNode()
        {
            var categoryAttr = GetType().GetCustomAttribute<NodeCategoryAttribute>();
            if (categoryAttr != null)
            {
                Category = categoryAttr.Category;
                if (!string.IsNullOrEmpty(categoryAttr.DisplayName))
                    Name = categoryAttr.DisplayName;
            }

            if (string.IsNullOrEmpty(Name))
                Name = GetType().Name;

            DiscoverPins();
        }

        private NodeMetadata GetMetadata()
        {
            return _metadataCache.GetOrAdd(GetType(), t =>
            {
                var meta = new NodeMetadata();
                meta.IsBackground = t.GetCustomAttribute<NodeBackgroundAttribute>() != null;
                meta.Properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var p in meta.Properties)
                {
                    meta.PropByName[p.Name] = p;

                    var inputAttr = p.GetCustomAttribute<NodeInputAttribute>();
                    if (inputAttr != null)
                        meta.InputAttrs[p] = inputAttr;

                    var outputAttr = p.GetCustomAttribute<NodeOutputAttribute>();
                    if (outputAttr != null)
                        meta.OutputAttrs[p] = outputAttr;

                    var propAttr = p.GetCustomAttribute<NodePropertyAttribute>();
                    if (propAttr != null)
                        meta.PropAttrs[p] = propAttr;
                }

                return meta;
            });
        }

        private void DiscoverPins()
        {
            var meta = GetMetadata();

            foreach (var kv in meta.InputAttrs)
            {
                var prop = kv.Key;
                var attr = kv.Value;
                var pin = new InputPin(
                    attr.DisplayName ?? prop.Name,
                    prop.PropertyType,
                    this,
                    attr.Required,
                    userDefined: false);
                Inputs.Add(pin);
            }

            foreach (var kv in meta.OutputAttrs)
            {
                var prop = kv.Key;
                var attr = kv.Value;
                var pin = new OutputPin(
                    attr.DisplayName ?? prop.Name,
                    prop.PropertyType,
                    this,
                    userDefined: false);
                Outputs.Add(pin);
            }
        }

        public List<NodePropertyDescriptor> GetNodeProperties()
        {
            var result = new List<NodePropertyDescriptor>();
            var meta = GetMetadata();

            foreach (var kv in meta.PropAttrs)
            {
                var prop = kv.Key;
                var attr = kv.Value;

                result.Add(new NodePropertyDescriptor
                {
                    Name = prop.Name,
                    DisplayName = attr.DisplayName ?? prop.Name,
                    Category = attr.Category ?? Category,
                    PropertyType = prop.PropertyType,
                    DefaultValue = attr.DefaultValue ?? GetDefault(prop.PropertyType),
                    Min = attr.Min,
                    Max = attr.Max,
                    Description = attr.Description,
                    Getter = () => prop.GetValue(this),
                    Setter = (v) => prop.SetValue(this, ConvertValue(prop.PropertyType, v)),
                });
            }

            return result;
        }

        public void SetNodeProperty(string name, object value)
        {
            var meta = GetMetadata();
            if (meta.PropByName.TryGetValue(name, out var prop))
            {
                var converted = ConvertValue(prop.PropertyType, value);
                prop.SetValue(this, converted);
            }
        }

        public object GetNodeProperty(string name)
        {
            var meta = GetMetadata();
            if (meta.PropByName.TryGetValue(name, out var prop))
                return prop.GetValue(this);
            return null;
        }

        public void BindInputsToProperties(FlowContext context)
        {
            var meta = GetMetadata();

            foreach (var kv in meta.InputAttrs)
            {
                var prop = kv.Key;
                var attr = kv.Value;

                var pin = Inputs.Find(p => p.Name == (attr.DisplayName ?? prop.Name));
                if (pin == null || !pin.IsConnected) continue;

                var val = pin.GetValue(context);
                if (val != null)
                {
                    prop.SetValue(this, ConvertValue(prop.PropertyType, val));
                }
            }
        }

        public void WriteOutputsFromProperties(FlowContext context)
        {
            var meta = GetMetadata();

            foreach (var kv in meta.OutputAttrs)
            {
                var prop = kv.Key;
                var attr = kv.Value;

                var pin = Outputs.Find(p => p.Name == (attr.DisplayName ?? prop.Name));
                if (pin != null)
                {
                    var val = prop.GetValue(this);
                    pin.SetValue(context, val);
                }
            }
        }

        public abstract Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken);

        public InputPin FindInput(string name)
        {
            return Inputs.Find(p => p.Name == name);
        }

        public OutputPin FindOutput(string name)
        {
            return Outputs.Find(p => p.Name == name);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        public event Action<FlowNode> OnPinsChanged;

        public InputPin AddInputPin(string name, Type dataType)
        {
            var existing = Inputs.Find(p => p.Name == name);
            if (existing != null) return existing;

            var pin = new InputPin(name, dataType, this);
            Inputs.Add(pin);
            OnPinsChanged?.Invoke(this);
            return pin;
        }

        public OutputPin AddOutputPin(string name, Type dataType)
        {
            var existing = Outputs.Find(p => p.Name == name);
            if (existing != null) return existing;

            var pin = new OutputPin(name, dataType, this);
            Outputs.Add(pin);
            OnPinsChanged?.Invoke(this);
            return pin;
        }

        public void RemovePin(string name)
        {
            var inp = Inputs.Find(p => p.Name == name);
            if (inp != null)
            {
                inp.Disconnect();
                Inputs.Remove(inp);
            }

            var outp = Outputs.Find(p => p.Name == name);
            if (outp != null)
            {
                foreach (var t in outp.Targets.ToList())
                    t.Disconnect();
                Outputs.Remove(outp);
            }

            OnPinsChanged?.Invoke(this);
        }

        private static object ConvertValue(Type targetType, object value)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            try
            {
                if (targetType == typeof(string)) return value.ToString();

                if (targetType.IsEnum)
                {
                    if (value is string s)
                        return Enum.Parse(targetType, s);
                    return Enum.ToObject(targetType, value);
                }

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return null;
            }
        }

        internal class NodeMetadata
        {
            public bool IsBackground;
            public PropertyInfo[] Properties;
            public readonly Dictionary<PropertyInfo, NodeInputAttribute> InputAttrs
                = new Dictionary<PropertyInfo, NodeInputAttribute>();
            public readonly Dictionary<PropertyInfo, NodeOutputAttribute> OutputAttrs
                = new Dictionary<PropertyInfo, NodeOutputAttribute>();
            public readonly Dictionary<PropertyInfo, NodePropertyAttribute> PropAttrs
                = new Dictionary<PropertyInfo, NodePropertyAttribute>();
            public readonly Dictionary<string, PropertyInfo> PropByName
                = new Dictionary<string, PropertyInfo>();
        }
    }

    public class NodePropertyDescriptor
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public Type PropertyType { get; set; }
        public object DefaultValue { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public string Description { get; set; }
        public Func<object> Getter { get; set; }
        public Action<object> Setter { get; set; }
    }
}
