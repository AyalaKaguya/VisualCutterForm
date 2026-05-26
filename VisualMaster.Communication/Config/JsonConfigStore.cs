using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using VisualMaster.Config.Abstractions;

namespace VisualMaster.Communication.Config
{
    public class JsonConfigStore : IConfigStore
    {
        private readonly string _filePath;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public JsonConfigStore(string filePath)
        {
            _filePath = filePath ?? throw new System.ArgumentNullException(nameof(filePath));
        }

        public T Load<T>(string sectionKey) where T : class, IConfigSection, new()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return _serializer.Deserialize<T>(json) ?? new T();
                }
            }
            catch { }
            return new T();
        }

        public void Save<T>(T section) where T : class, IConfigSection
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = _serializer.Serialize(section);
            File.WriteAllText(_filePath, json);
        }

        public async Task SaveAsync<T>(T section, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IConfigSection
        {
            await Task.Run(() => Save(section), cancellationToken).ConfigureAwait(false);
        }
    }
}
