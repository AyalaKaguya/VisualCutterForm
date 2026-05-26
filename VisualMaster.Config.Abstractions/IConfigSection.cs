namespace VisualMaster.Config.Abstractions
{
    public interface IConfigSection
    {
        string SectionKey { get; }
        int Version { get; }
    }
}
