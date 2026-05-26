namespace VisualMaster.Config.Abstractions
{
    public interface IConfigSerializer
    {
        string Serialize<T>(T section) where T : class, IConfigSection;
        T Deserialize<T>(string data) where T : class, IConfigSection, new();
    }
}
