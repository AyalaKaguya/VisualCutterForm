namespace VisualMaster.Api.Configuration
{
    public interface IConfigurationSerializer
    {
        string Serialize<T>(T value) where T : class, new();

        T Deserialize<T>(string data) where T : class, new();
    }
}
