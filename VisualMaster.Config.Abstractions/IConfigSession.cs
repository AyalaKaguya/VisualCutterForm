namespace VisualMaster.Config.Abstractions
{
    public interface IConfigSession<T> where T : class, IConfigSection
    {
        T Current { get; }
        T Snapshot { get; }
        bool HasChanges { get; }
        void Load(T config);
        void Commit();
        void Revert();
    }
}
