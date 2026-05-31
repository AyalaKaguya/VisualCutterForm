using System.Text.Json;
using VisualMaster.Api.Configuration;

namespace VisualMaster.Impl.Configuration
{
    public sealed class SystemTextJsonConfigurationSerializer : IConfigurationSerializer
    {
        private readonly JsonSerializerOptions _options;

        public SystemTextJsonConfigurationSerializer()
        {
            _options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public string Serialize<T>(T value) where T : class, new()
        {
            return JsonSerializer.Serialize(value, _options);
        }

        public T Deserialize<T>(string data) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return new T();
            }

            return JsonSerializer.Deserialize<T>(data, _options) ?? new T();
        }
    }
}
