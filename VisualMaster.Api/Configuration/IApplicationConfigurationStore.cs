using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.Api.Configuration
{
    public interface IApplicationConfigurationStore
    {
        T Load<T>() where T : class, new();

        Task<T> LoadAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : class, new();

        T LoadOrCreate<T>() where T : class, new();

        void Save<T>(T configuration) where T : class, new();

        Task SaveAsync<T>(T configuration, CancellationToken cancellationToken = default(CancellationToken)) where T : class, new();

        void Apply<T>(T target) where T : class, new();

        string GetPath<T>() where T : class, new();
    }
}
