using Atheon.Models.Database.Administration;
using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Models.Database.Destiny.Clans;
using Atheon.Models.Database.Destiny.Guilds;
using Atheon.Models.Database.Destiny.Links;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Models.Database.Destiny.Tracking;

namespace Atheon.Services.Interfaces;

public interface IDestinyDb
{
    Task<List<GuildReference>> GetGuildReferencesAsync();
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettings();
    Task<DiscordGuildSettingsDbModel?> GetGuildSettingsAsync(ulong guildId);
    Task UpsertGuildSettingsAsync(DiscordGuildSettingsDbModel guildSettings);
    Task DeleteGuildSettingsAsync(ulong guildId);
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettingsForClanAsync(long clanId);
    Task<List<ClanReference>> GetClanReferencesFromGuildAsync(ulong guildId);

    Task<List<long>> GetClanIdsAsync(bool isTracking, DateTime oldenThan);
    Task<DestinyClanDbModel?> GetClanModelAsync(long clanId);
    Task UpsertClanModelAsync(DestinyClanDbModel clanDbModel);
    Task<List<ClanMemberReference>> GetClanMemberReferencesAsync(long clanId);
    Task<List<ClanReference>> GetClanReferencesFromIdsAsync(long[] clanIds);

    Task<DestinyProfileDbModel?> GetDestinyProfileAsync(long membershipId);
    Task UpsertDestinyProfileAsync(DestinyProfileDbModel profileDbModel);
    Task<string?> GetProfileDisplayNameAsync(long membershipId);
    Task DeleteDestinyProfileAsync(long membershipId);

    Task<List<DestinyProfileLite>> GetProfilesCollectibleStatusAsync(uint collectibleHash, bool hasItem);
    Task<List<DestinyProfileLite>> GetProfilesRecordStatusAsync(uint recordHash, bool hasCompletedRecord);

    Task TryInsertClanBroadcastAsync(ClanBroadcastDbModel clanBroadcast);
    Task TryInsertProfileBroadcastAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);

    Task MarkClanBroadcastSentAsync(ClanBroadcastDbModel clanBroadcast);
    Task MarkUserBroadcastSentAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);

    Task UpsertCuratedRecordDefinitionAsync(CuratedRecord curatedRecord);
    Task UpsertCuratedCollectibleDefinitionAsync(CuratedCollectible curatedCollectible);

    Task<List<CuratedRecord>> GetCuratedRecordsAsync();
    Task<List<CuratedCollectible>> GetCuratedCollectiblesAsync();

    Task UpsertProfileLinkAsync(DiscordToDestinyProfileLink link);
    Task RemoveProfileLinkAsync(ulong discordUserId);

    Task AddServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator);
    Task RemoveServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator);
    Task<bool> IsServerAdministratorAsync(ulong guildId, ulong userId);
}
