using System.Collections.Frozen;

namespace ServerPathsMinimalApi.Services.Interfaces
{
    public interface IFileProviderService
    {
        FrozenSet<string> CachedFiles { get; }
    }
}