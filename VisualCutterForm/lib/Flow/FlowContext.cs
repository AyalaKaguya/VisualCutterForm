using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VisualCutterForm.Lib.Flow
{
    public class FlowContext
    {
        private readonly ConcurrentDictionary<Guid, object> _pinValues = new ConcurrentDictionary<Guid, object>();
        private readonly ConcurrentDictionary<string, object> _variables = new ConcurrentDictionary<string, object>();
        private readonly string _subGraphId;

        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;
        public event Action<string> OnLogError;

        public FlowContext(string subGraphId = null)
        {
            _subGraphId = subGraphId ?? Guid.NewGuid().ToString();
        }

        public void Log(string message)
        {
            OnLog?.Invoke(message);
        }

        public void LogWarning(string message)
        {
            OnLogWarning?.Invoke(message);
        }

        public void LogError(string message)
        {
            OnLogError?.Invoke(message);
        }

        public void SetPinValue(OutputPin pin, object value)
        {
            if (pin == null) return;
            _pinValues[pin.Id] = value;
        }

        public object GetPinValue(OutputPin pin)
        {
            if (pin == null) return null;
            _pinValues.TryGetValue(pin.Id, out var val);
            return val;
        }

        public bool TryGetPinValue(InputPin pin, out object value)
        {
            value = null;
            if (pin?.Source == null) return false;
            return _pinValues.TryGetValue(pin.Source.Id, out value);
        }

        public void SetVariable(string key, object value)
        {
            _variables[key] = value;
        }

        public T GetVariable<T>(string key, T defaultValue = default(T))
        {
            if (_variables.TryGetValue(key, out var val) && val is T t)
                return t;
            return defaultValue;
        }

        public void Clear()
        {
            _pinValues.Clear();
            _variables.Clear();
        }
    }
}
