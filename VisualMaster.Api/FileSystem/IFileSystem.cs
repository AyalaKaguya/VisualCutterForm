using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.Api.FileSystem
{
    public interface IFileSystem
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        string ReadAllText(string path);

        Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);

        void WriteAllText(string path, string contents);

        Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken);

        string Combine(params string[] paths);

        string GetFullPath(string path);

        string GetDirectoryName(string path);
    }
}
