using System;
using VisualMaster.Config.Abstractions;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.Config
{
    public class CommunicationConfigService
    {
        private readonly IConfigStore _store;
        private CommunicationManager _manager;
        private ConfigSession<CommunicationConfigSection> _session;

        public CommunicationConfigSection CurrentConfig => _session?.Current;
        public ConfigSession<CommunicationConfigSection> Session => _session;

        public CommunicationConfigService(IConfigStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public CommunicationConfigSection LoadConfig()
        {
            var config = _store.Load<CommunicationConfigSection>("communication");
            _session = new ConfigSession<CommunicationConfigSection>(_store);
            _session.Load(config);
            return config;
        }

        public void LoadConfigFrom(CommunicationConfigSection config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _session = new ConfigSession<CommunicationConfigSection>(_store);
            _session.Load(config);
        }

        public void BindToManager(CommunicationManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            if (_session?.Current != null)
                _manager.LoadConfig((CommunicationConfigSection)_session.Current.Clone());
        }

        public void SyncToManager()
        {
            if (_manager != null && _session?.Current != null)
                _manager.LoadConfig((CommunicationConfigSection)_session.Current.Clone());
        }

        public CommunicationConfigViewModel CreateViewModel()
        {
            if (_session == null)
                throw new InvalidOperationException("请先调用 LoadConfig() 再创建 ViewModel。");
            return new CommunicationConfigViewModel(_session, OnConfigChanged);
        }

        private void OnConfigChanged(CommunicationConfigSection config)
        {
            if (_manager != null && config != null)
                _manager.LoadConfig((CommunicationConfigSection)config.Clone());
        }
    }
}
