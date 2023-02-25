using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Models.Database.Destiny.Clans;
using Atheon.Models.Database.Destiny.Guilds;
using Atheon.Models.Database.Destiny.Profiles;

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
    Task<List<ClanMemberReference>> GetClanMemberReferencesAsync(long clanId);

    Task<DestinyProfileDbModel?> GetDestinyProfileAsync(long membershipId);
    Task UpsertDestinyProfileAsync(DestinyProfileDbModel profileDbModel);
    Task<string?> GetProfileDisplayNameAsync(long membershipId);
    Task DeleteDestinyProfileAsync(long membershipId);

    Task<List<DestinyProfileLite>> GetProfilesCollectibleStatusAsync(uint collectibleHash, bool hasItem);

    Task TryInsertClanBroadcastAsync(ClanBroadcastDbModel clanBroadcast);
    Task TryInsertProfileBroadcastAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);

    Task MarkClanBroadcastSentAsync(ClanBroadcastDbModel clanBroadcast);
    Task MarkUserBroadcastSentAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);
}
