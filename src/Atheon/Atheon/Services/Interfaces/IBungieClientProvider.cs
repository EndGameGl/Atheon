using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Interfaces;

public interface IBungieClientProvider
{
    bool IsReady { get; }
    ValueTask<IBungieClient> GetClientAsync();
    void SetApiKey(string apiKey);
    Task SetManifestPath(string path, bool reloadRepository);
}
