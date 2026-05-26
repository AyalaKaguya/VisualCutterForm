using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.Config.Abstractions
{
    public class ConfigRegistry
    {
        private readonly Dictionary<string, IModuleConfigProvider> _providers =
            new Dictionary<string, IModuleConfigProvider>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<IModuleConfigProvider> Providers => _providers.Values.ToList().AsReadOnly();

        public void Register(IModuleConfigProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _providers[provider.SectionKey] = provider;
        }

        public IModuleConfigProvider GetProvider(string sectionKey)
        {
            _providers.TryGetValue(sectionKey, out var provider);
            return provider;
        }

        public IConfigSection CreateDefault(string sectionKey)
        {
            return _providers.TryGetValue(sectionKey, out var provider)
                ? provider.CreateDefault()
                : null;
        }
    }

    public interface IModuleConfigProvider
    {
        string SectionKey { get; }
        Type ConfigType { get; }
        IConfigSection CreateDefault();
    }
}
