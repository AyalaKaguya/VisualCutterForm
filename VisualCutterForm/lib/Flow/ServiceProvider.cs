using System;
using System.Collections.Generic;

namespace VisualCutterForm.Lib.Flow
{
    public class ServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<ServiceProvider, object>> _factories
            = new Dictionary<Type, Func<ServiceProvider, object>>();

        public void Register<T>(T instance)
        {
            _services[typeof(T)] = instance;
        }

        public void Register<T>(Func<ServiceProvider, T> factory)
        {
            _factories[typeof(T)] = sp => factory(sp);
        }

        public T Resolve<T>()
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var instance))
                return (T)instance;
            if (_factories.TryGetValue(type, out var factory))
            {
                var created = (T)factory(this);
                _services[type] = created;
                return created;
            }
            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }

        public bool TryResolve<T>(out T service)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var instance))
            {
                service = (T)instance;
                return true;
            }
            if (_factories.TryGetValue(type, out var factory))
            {
                service = (T)factory(this);
                _services[type] = service;
                return true;
            }
            service = default;
            return false;
        }
    }
}
