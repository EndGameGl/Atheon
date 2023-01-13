using Atheon.Models.Destiny;
using Atheon.Services.Interfaces;
using Discord.WebSocket;

namespace Atheon.Services.Db.Sqlite
{
    public class SqliteDbDataValidator : IDbDataValidator
    {
        private readonly IDbAccess _dbAccess;
        private readonly IDiscordClientProvider _discordClientProvider;
        private readonly ILogger<SqliteDbDataValidator> _logger;

        public SqliteDbDataValidator(
            IDbAccess dbAccess,
            IDiscordClientProvider discordClientProvider,
            ILogger<SqliteDbDataValidator> logger)
        {
            _dbAccess = dbAccess;
            _discordClientProvider = discordClientProvider;
            _logger = logger;
        }


        public async Task ValidateDbData()
        {
            if (!_discordClientProvider.IsReady)
            {
                _logger.LogWarning("Discord client wasn't ready for DB data validation");
                return;
            }

            var discordClient = _discordClientProvider.Client!;

            var guilds = discordClient.Guilds.ToList();
            var savedGuildSettings = await _dbAccess.QueryAsync<GuildSettings>("SELECT * FROM Guilds");
            await AddMissingDbSettings(guilds, savedGuildSettings);
            await RemoveObsoleteSettingsFromDb(guilds, savedGuildSettings);
        }

        private async Task AddMissingDbSettings(List<SocketGuild> guilds, List<GuildSettings> savedGuildSettings)
        {
            foreach (var guild in guilds)
            {
                if (!savedGuildSettings.Any(x => x.GuildId == guild.Id))
                {
                    await _dbAccess.UpsertGuildSettingsAsync(GuildSettings.CreateDefault(guild.Id, guild.Name));
                }
            }
        }

        private async Task RemoveObsoleteSettingsFromDb(List<SocketGuild> guilds, List<GuildSettings> savedGuildSettings)
        {
            foreach (var savedGuildSetting in savedGuildSettings)
            {
                if (!guilds.Any(x => x.Id == savedGuildSetting.GuildId))
                {
                    await _dbAccess.DeleteGuildSettingsAsync(savedGuildSetting.GuildId);
                }
            }
        }
    }
}
