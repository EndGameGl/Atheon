using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Models.Database.Destiny.Guilds;

namespace Atheon.Services.Interfaces;

public interface IDestinyDb
{
    Task<List<GuildReference>> GetGuildReferencesAsync();
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettings();
    Task<DiscordGuildSettingsDbModel?> GetGuildSettingsAsync(ulong guildId);
    Task UpsertGuildSettingsAsync(DiscordGuildSettingsDbModel guildSettings);
    Task DeleteGuildSettingsAsync(ulong guildId);
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettingsForClanAsync(long clanId);

    Task<List<long>> GetClanIdsAsync(bool isTracking, DateTime oldenThan);
    Task<DestinyClanDbModel?> GetClanModelAsync(long clanId);
    Task UpsertClanModelAsync(DestinyClanDbModel clanDbModel);

    Task<DestinyProfileDbModel?> GetDestinyProfileAsync(long membershipId);
    Task UpsertDestinyProfileAsync(DestinyProfileDbModel profileDbModel);

    Task<List<DestinyProfileDbModel>> GetProfilesWithCollectibleAsync(uint collectibleHash);

    Task TryInsertClanBroadcastAsync(ClanBroadcastDbModel clanBroadcast);
    Task TryInsertProfileBroadcastAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);
}
