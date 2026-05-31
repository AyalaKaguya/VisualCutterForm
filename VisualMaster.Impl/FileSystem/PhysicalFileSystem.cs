using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Api.FileSystem;

namespace VisualMaster.Impl.FileSystem
{
    public sealed class PhysicalFileSystem : IFileSystem
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            return Task.Run(() => File.ReadAllText(path), cancellationToken);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
        {
            return Task.Run(() => File.WriteAllText(path, contents), cancellationToken);
        }

        public string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}
