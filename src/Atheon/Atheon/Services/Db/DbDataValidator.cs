using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny;
using Atheon.Services.Interfaces;
using Discord.WebSocket;

namespace Atheon.Services.Db.Sqlite
{
    public class DbDataValidator : IDbDataValidator
    {
        private readonly IDestinyDb _destinyDb;
        private readonly IDiscordClientProvider _discordClientProvider;
        private readonly IGuildDb _guildDb;
        private readonly ILogger<DbDataValidator> _logger;

        public DbDataValidator(
            IDestinyDb destinyDb,
            IDiscordClientProvider discordClientProvider,
            IGuildDb guildDb,
            ILogger<DbDataValidator> logger)
        {
            _destinyDb = destinyDb;
            _discordClientProvider = discordClientProvider;
            _guildDb = guildDb;
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
            var savedGuildSettings = await _guildDb.GetAllGuildSettings();
            await AddMissingDbSettings(guilds, savedGuildSettings);
            await RemoveObsoleteSettingsFromDb(guilds, savedGuildSettings);
        }

        private async Task AddMissingDbSettings(List<SocketGuild> guilds, List<DiscordGuildSettingsDbModel> savedGuildSettings)
        {
            foreach (var guild in guilds)
            {
                if (!savedGuildSettings.Any(x => x.GuildId == guild.Id))
                {
                    await _guildDb.UpsertGuildSettingsAsync(DiscordGuildSettingsDbModel.CreateDefault(guild.Id, guild.Name));
                }
            }
        }

        private async Task RemoveObsoleteSettingsFromDb(List<SocketGuild> guilds, List<DiscordGuildSettingsDbModel> savedGuildSettings)
        {
            foreach (var savedGuildSetting in savedGuildSettings)
            {
                if (!guilds.Any(x => x.Id == savedGuildSetting.GuildId))
                {
                    await _guildDb.DeleteGuildSettingsAsync(savedGuildSetting.GuildId);
                }
            }
        }
    }
}
