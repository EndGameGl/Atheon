using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Interfaces;

public interface IBungieClientProvider
{
    ValueTask<IBungieClient> GetClientAsync();
    void SetApiKey(string apiKey);
    Task SetManifestPath(string path, bool reloadRepository);
}
