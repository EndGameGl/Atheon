using Atheon.DataAccess.Models.Destiny.Clans;
using Atheon.DataAccess.Models.Destiny.Guilds;
using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Discord;

namespace Atheon.DataAccess;

public interface IGuildDb
{
    Task<List<GuildReference>> GetGuildReferencesAsync();
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettings();
    Task<DiscordGuildSettingsDbModel?> GetGuildSettingsAsync(ulong guildId);
    Task UpsertGuildSettingsAsync(DiscordGuildSettingsDbModel guildSettings);
    Task DeleteGuildSettingsAsync(ulong guildId);
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettingsForClanAsync(long clanId);
    Task<List<ClanReference>> GetClanReferencesFromGuildAsync(ulong guildId);
    Task<DiscordDestinyLanguageEnum> GetGuildLanguageAsync(ulong guildId);
}
