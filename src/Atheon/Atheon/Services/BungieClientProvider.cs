using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.Interfaces;
using DotNetBungieAPI;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Service.Abstractions;
using Serilog;

namespace Atheon.Services;

public class BungieClientProvider : IBungieClientProvider
{
    private readonly ISettingsStorage _settingsStorage;
    private readonly IDestinyDb _destinyDb;
    private IBungieClient? _clientInstance;
    private IBungieClientConfiguration? _bungieClientConfiguration;
    private SqliteDefinitionProviderConfiguration? _providerConfiguration;

    public bool IsReady { get; private set; }

    public BungieClientProvider(
        ISettingsStorage settingsStorage,
        IDestinyDb destinyDb)
    {
        _settingsStorage = settingsStorage;
        _destinyDb = destinyDb;
    }

    private async Task<IBungieClient> ResolveClientInstance()
    {
        var apiKey = await _settingsStorage.GetBungieApiKey();

        Ensure.That(apiKey).Is(key => key.IsNotNullOrEmpty(), errorMessage: "Api key for bungie.net apps can't be empty");

        var manifestPath = await _settingsStorage.GetManifestPath();

        Ensure.That(manifestPath).Is(path => path.IsNotNullOrEmpty(), errorMessage: "Manifest path can't be empty");

        var client = await CreateClient(apiKey, manifestPath);

        IsReady = true;

        return client;
    }

    private async Task<IBungieClient> CreateClient(string apiKey, string manifestPath)
    {
        var languages = (await _destinyDb.GetAllGuildSettings()).Select(x => x.DestinyManifestLocale.ConvertToBungieLocale()).Distinct().ToList();

        if (!languages.Contains(BungieLocales.EN))
        {
            languages.Add(BungieLocales.EN);
        }

        var services = new ServiceCollection();

        services.AddLogging(x => x.AddSerilog());

        services.UseBungieApiClient((config) =>
        {
            config.ClientConfiguration.ApiKey = apiKey;
            config.ClientConfiguration.UsedLocales.AddRange(languages);
            config.DefinitionProvider.UseSqliteDefinitionProvider(provider =>
            {
                provider.ManifestFolderPath = manifestPath;
                provider.DeleteOldManifestDataAfterUpdates = true;
                provider.AutoUpdateManifestOnStartup = true;
            });
            config.DefinitionRepository.ConfigureDefaultRepository(repository =>
            {
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinySackRewardItemListDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyTalentGridDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyTraitCategoryDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyAchievementDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyBondDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyUnlockDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyUnlockValueDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyRewardSourceDefinition);
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        _bungieClientConfiguration = serviceProvider.GetRequiredService<IBungieClientConfiguration>();
        _providerConfiguration = serviceProvider.GetRequiredService<SqliteDefinitionProviderConfiguration>();

        var client = serviceProvider.GetRequiredService<IBungieClient>();

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

    public async Task ReloadClient()
    {
        _clientInstance = await ResolveClientInstance();
        await _clientInstance.DefinitionProvider.Initialize();
        await _clientInstance.DefinitionProvider.ReadToRepository(_clientInstance.Repository);
    }
}
