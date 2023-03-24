using Atheon.DataAccess;
using Atheon.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace Atheon.Services.DiscordHandlers
{
    public class DiscordClientProvider : IDiscordClientProvider
    {
        private DiscordShardedClient? _client;

        private TaskCompletionSource? _completionSource;
        private int? _shardsReady;
        private readonly ILogger<DiscordClientProvider> _logger;
        private readonly ISettingsStorage _settingsStorage;

        public event Func<Task> ClientReloaded;

        public DiscordClientProvider(
            ILogger<DiscordClientProvider> logger,
            ISettingsStorage settingsStorage)
        {
            _logger = logger;
            _settingsStorage = settingsStorage;
        }

        public bool IsReady { get; private set; }

        public DiscordShardedClient? Client => _client;

        public async Task ConnectAsync()
        {
            if (_client is not null || IsReady)
            {
                return;
            }

            var discordToken = await _settingsStorage.GetOption<string?>(SettingKeys.DiscordToken);
            if (discordToken is null)
            {
                _logger.LogWarning("Expected to see discord token set in settings!");
                return;
            }

            _logger.LogInformation("Creating discord client...");

            _client = new DiscordShardedClient();

            PrepareClientAwaiter();

            _logger.LogInformation("Logging in to discord client...");
            await _client.LoginAsync(TokenType.Bot, discordToken);

            _logger.LogInformation("Starting discord client...");
            await _client.StartAsync();

            await WaitForReadyAsync();
            _logger.LogInformation("Discord client is ready!");
            IsReady = true;
        }

        private Task WaitForReadyAsync()
        {
            return _completionSource!.Task;
        }

        private void PrepareClientAwaiter()
        {
            _completionSource = new TaskCompletionSource();
            _shardsReady = 0;
            _client!.ShardReady += OnClientShardReady;
        }

        private Task OnClientShardReady(DiscordSocketClient clientShard)
        {
            _logger.LogInformation("Discord shard {Number} is ready!", clientShard.ShardId);
            _shardsReady++;
            if (_shardsReady == _client!.Shards.Count)
            {
                _completionSource?.TrySetResult();
                _client.ShardReady -= OnClientShardReady;
            }
            return Task.CompletedTask;
        }

        public async Task ForceReloadClientAsync()
        {
            IsReady = false;
            if (_client is not null)
            {
                await _client.StopAsync();
                await _client.DisposeAsync();
                _client = null;
            }
            await ConnectAsync();
        }
    }
}
