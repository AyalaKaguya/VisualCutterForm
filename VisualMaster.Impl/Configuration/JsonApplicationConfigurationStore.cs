using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Api.Application;
using VisualMaster.Api.Configuration;
using VisualMaster.Api.FileSystem;

namespace VisualMaster.Impl.Configuration
{
    public sealed class JsonApplicationConfigurationStore : IApplicationConfigurationStore
    {
        private readonly IApplicationBootstrapContext _context;
        private readonly IFileSystem _fileSystem;
        private readonly IConfigurationSerializer _serializer;

        public JsonApplicationConfigurationStore(
            IApplicationBootstrapContext context,
            IFileSystem fileSystem,
            IConfigurationSerializer serializer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public T Load<T>() where T : class, new()
        {
            var path = GetPath<T>();
            if (!_fileSystem.FileExists(path))
            {
                return new T();
            }

            return _serializer.Deserialize<T>(_fileSystem.ReadAllText(path));
        }

        public async Task<T> LoadAsync<T>(CancellationToken cancellationToken = default(CancellationToken))
            where T : class, new()
        {
            var path = GetPath<T>();
            if (!_fileSystem.FileExists(path))
            {
                return new T();
            }

            var data = await _fileSystem.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return _serializer.Deserialize<T>(data);
        }

        public T LoadOrCreate<T>() where T : class, new()
        {
            var path = GetPath<T>();
            if (_fileSystem.FileExists(path))
            {
                return Load<T>();
            }

            var configuration = new T();
            Save(configuration);
            return configuration;
        }

        public void Save<T>(T configuration) where T : class, new()
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var path = GetPath<T>();
            EnsureParentDirectory(path);
            _fileSystem.WriteAllText(path, _serializer.Serialize(configuration));
        }

        public async Task SaveAsync<T>(T configuration, CancellationToken cancellationToken = default(CancellationToken))
            where T : class, new()
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var path = GetPath<T>();
            EnsureParentDirectory(path);
            await _fileSystem.WriteAllTextAsync(path, _serializer.Serialize(configuration), cancellationToken)
                .ConfigureAwait(false);
        }

        public void Apply<T>(T target) where T : class, new()
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var source = LoadOrCreate<T>();
            CopyProperties(source, target);
        }

        public string GetPath<T>() where T : class, new()
        {
            var relativePath = ResolveRelativePath(typeof(T));
            var configRoot = ResolveRootPath(_context.Paths.ConfigRoot);
            return _fileSystem.GetFullPath(_fileSystem.Combine(configRoot, relativePath));
        }

        private string ResolveRelativePath(Type configurationType)
        {
            var attribute = configurationType
                .GetCustomAttributes(typeof(ConfigurationFileAttribute), false)
                .OfType<ConfigurationFileAttribute>()
                .FirstOrDefault();

            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.RelativePath))
            {
                return attribute.RelativePath;
            }

            return configurationType.Name + ".json";
        }

        private string ResolveRootPath(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                root = "config";
            }

            if (System.IO.Path.IsPathRooted(root))
            {
                return root;
            }

            return _fileSystem.Combine(_context.BaseDirectory, root);
        }

        private void EnsureParentDirectory(string path)
        {
            var directory = _fileSystem.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }
        }

        private static void CopyProperties<T>(T source, T target)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                property.SetValue(target, property.GetValue(source, null), null);
            }
        }
    }
}
