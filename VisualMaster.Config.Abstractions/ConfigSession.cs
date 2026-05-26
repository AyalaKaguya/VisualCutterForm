using System;

namespace VisualMaster.Config.Abstractions
{
    public class ConfigSession<T> : IConfigSession<T> where T : class, IConfigSection, ICloneable
    {
        private readonly IConfigStore _store;

        public ConfigSession(IConfigStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public T Current { get; private set; }
        public T Snapshot { get; private set; }

        public bool HasChanges
        {
            get
            {
                if (Current == null || Snapshot == null) return true;
                return !Snapshot.Equals(Current);
            }
        }

        public void Load(T config)
        {
            Current = (T)config.Clone();
            Snapshot = (T)config.Clone();
        }

        public void Commit()
        {
            if (Current == null) return;
            _store.Save(Current);
            Snapshot = (T)Current.Clone();
        }

        public void Revert()
        {
            if (Snapshot == null) return;
            Current = (T)Snapshot.Clone();
        }
    }
}
