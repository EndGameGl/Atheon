using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;

namespace Atheon.Services.Hosted
{
    public class ApplicationStartup : BackgroundService
    {
        private readonly IDiscordClientProvider _discordClientProvider;
        private readonly IDbBootstrap _dbBootstrap;
        private readonly ISettingsStorage _settingsStorage;
        private readonly IBungieClientProvider _bungieClientProvider;
        private readonly ILogger<ApplicationStartup> _logger;
        private readonly IDiscordEventHandler _discordEventHandler;
        private readonly IDbDataValidator _dbDataValidator;
        private readonly CuratedDefinitionInitialiser _curatedDefinitionInitialiser;
        private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

        public ApplicationStartup(
            IDiscordClientProvider discordClientProvider,
            IDbBootstrap dbBootstrap,
            ISettingsStorage settingsStorage,
            IBungieClientProvider bungieClientProvider,
            ILogger<ApplicationStartup> logger,
            IDiscordEventHandler discordEventHandler,
            IDbDataValidator dbDataValidator,
            CuratedDefinitionInitialiser curatedDefinitionInitialiser,
            DestinyDefinitionDataService destinyDefinitionDataService)
        {
            _discordClientProvider = discordClientProvider;
            _dbBootstrap = dbBootstrap;
            _settingsStorage = settingsStorage;
            _bungieClientProvider = bungieClientProvider;
            _logger = logger;
            _discordEventHandler = discordEventHandler;
            _dbDataValidator = dbDataValidator;
            _curatedDefinitionInitialiser = curatedDefinitionInitialiser;
            _destinyDefinitionDataService = destinyDefinitionDataService;
        }

        private async Task RunInitialWarnings()
        {
            var discordToken = await _settingsStorage.GetDiscordToken();

            if (discordToken.IsNullOrEmpty())
            {
                _logger.LogWarning("Please set discord token in settings menu");
            }

            var apiKey = await _settingsStorage.GetBungieApiKey();

            if (apiKey.IsNullOrEmpty())
            {
                _logger.LogWarning("Please set bungie.net api key in settings menu");
            }

            var manifestPath = await _settingsStorage.GetManifestPath();

            if (manifestPath.IsNullOrEmpty())
            {
                _logger.LogWarning("Please set manifest directory path in settings menu");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _dbBootstrap.InitialiseDb(stoppingToken);

            await RunInitialWarnings();


            var client = await TryExecuteValue(async () => await _bungieClientProvider.GetClientAsync());

            await TryExecute(async () =>
            {
                if (client is null)
                    return;
                await client.DefinitionProvider.Initialize();
                await client.DefinitionProvider.ReadToRepository(client.Repository);
                await _destinyDefinitionDataService.MapLookupTables();

            });
            await TryExecute(async () =>
            {
                await _discordClientProvider.ConnectAsync();
            });

            await TryExecute(async () =>
            {
                await _dbDataValidator.ValidateDbData();
                await _curatedDefinitionInitialiser.Initialise();
            });

            await TryExecute(async () =>
            {
                _discordEventHandler.SubscribeToEvents();
            });
        }

        private async Task TryExecute(Func<Task> taskToExecute)
        {
            try
            {
                await taskToExecute();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run task on startup");
            }
        }

        private async Task<T?> TryExecuteValue<T>(Func<Task<T>> taskToExecute)
        {
            try
            {
                return await taskToExecute();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run task on startup");
                return default;
            }
        }
    }
}
