using System.Collections.Frozen;

namespace ServerPathsMinimalApi.Services.Interfaces
{
    public interface IFileProviderBgService
    {
        FrozenSet<string> GetCurrentFiles();
    }
}