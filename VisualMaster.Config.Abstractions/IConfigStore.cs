using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.Config.Abstractions
{
    public interface IConfigStore
    {
        T Load<T>(string sectionKey) where T : class, IConfigSection, new();
        void Save<T>(T section) where T : class, IConfigSection;
        Task SaveAsync<T>(T section, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IConfigSection;
    }
}
