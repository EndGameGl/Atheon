using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.Interfaces;
using DotNetBungieAPI;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.ReportReasonCategories;
using DotNetBungieAPI.Service.Abstractions;
using Serilog;
using System.Net;

namespace Atheon.Services;

public class BungieClientProvider : IBungieClientProvider
{
    private readonly ISettingsStorage _settingsStorage;
    private readonly IGuildDb _guildDb;
    private IBungieClient? _clientInstance;
    private IBungieClientConfiguration? _bungieClientConfiguration;
    private SqliteDefinitionProviderConfiguration? _providerConfiguration;

    public bool IsReady { get; private set; }

    public BungieClientProvider(
        ISettingsStorage settingsStorage,
        IGuildDb guildDb)
    {
        _settingsStorage = settingsStorage;
        _guildDb = guildDb;
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
        var languages = (await _guildDb.GetAllGuildSettings()).Select(x => x.DestinyManifestLocale.ConvertToBungieLocale()).Distinct().ToList();

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
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyTraitDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyAchievementDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyBondDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyUnlockDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyUnlockValueDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyRewardSourceDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyLoadoutColorDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyLoadoutConstantsDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyLoadoutIconDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyLoadoutNameDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyProgressionLevelRequirementDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyProgressionMappingDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyEnemyRaceDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyRaceDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyLoreDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyVendorDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyVendorGroupDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyGuardianRankDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyGuardianRankConstantsDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyEventCardDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinySocialCommendationDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinySocialCommendationNodeDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyPowerCapDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyMilestoneDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyMaterialRequirementSetDefinition);
                repository.IgnoreDefinitionType(DefinitionsEnum.DestinyReportReasonCategoryDefinition);
            });
            config.DotNetBungieApiHttpClient.ConfigureDefaultHttpClient(client =>
            {
                client.ConfigureHttpHandler = (httpHandler) =>
                {
                    httpHandler.PooledConnectionLifetime = TimeSpan.FromSeconds(15);
                    httpHandler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(45);
                    httpHandler.UseCookies = true;
                    httpHandler.CookieContainer = new CookieContainer();
                };
                client.ConfigureHttpClient = (httpClient) =>
                {
                    httpClient.DefaultRequestVersion = HttpVersion.Version20;
                };
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
