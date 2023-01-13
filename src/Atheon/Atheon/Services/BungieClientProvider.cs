using Atheon.Extensions;
using Atheon.Services.Interfaces;
using DotNetBungieAPI;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Service.Abstractions;
using Serilog;

namespace Atheon.Services;

public class BungieClientProvider : IBungieClientProvider
{
    private readonly ISettingsStorage _settingsStorage;

    private IBungieClient? _clientInstance;
    private IBungieClientConfiguration? _bungieClientConfiguration;
    private SqliteDefinitionProviderConfiguration? _providerConfiguration;

    public BungieClientProvider(
        ISettingsStorage settingsStorage)
    {
        _settingsStorage = settingsStorage;
    }

    private async Task<IBungieClient> ResolveClientInstance()
    {
        var apiKey = await _settingsStorage.GetBungieApiKey();

        Ensure.That(apiKey).Is(key => key.IsNotNullOrEmpty(), errorMessage: "Api key for bungie.net apps can't be empty");

        var manifestPath = await _settingsStorage.GetManifestPath();

        Ensure.That(manifestPath).Is(path => path.IsNotNullOrEmpty(), errorMessage: "Manifest path can't be empty");

        var services = new ServiceCollection();

        services.AddLogging(x => x.AddSerilog());

        var client = BungieApiBuilder.GetApiClient((config) =>
         {
             config.ClientConfiguration.ApiKey = apiKey;
             config.ClientConfiguration.UsedLocales.AddRange(new[] { BungieLocales.EN });
             config.DefinitionProvider.UseSqliteDefinitionProvider(provider =>
             {
                 provider.ManifestFolderPath = manifestPath;
                 provider.DeleteOldManifestDataAfterUpdates = true;
                 provider.AutoUpdateManifestOnStartup = true;
             });
         },
         services);

        var serviceProvider = services.BuildServiceProvider();

        _bungieClientConfiguration = serviceProvider.GetRequiredService<IBungieClientConfiguration>();
        _providerConfiguration = serviceProvider.GetRequiredService<SqliteDefinitionProviderConfiguration>();

        return client;
    }

    public async ValueTask<IBungieClient> GetClientAsync()
    {
        _clientInstance ??= await ResolveClientInstance();

        return _clientInstance;
    }

    public void SetApiKey(string apiKey)
    {
        _bungieClientConfiguration.ApiKey = apiKey;
    }

    public async Task SetManifestPath(string path, bool reloadRepository)
    {
        _providerConfiguration.ManifestFolderPath = path;
        await _clientInstance.DefinitionProvider.Initialize();
        if (reloadRepository)
        {
            await _clientInstance.DefinitionProvider.ReadToRepository(_clientInstance.Repository);
        }
    }
}
