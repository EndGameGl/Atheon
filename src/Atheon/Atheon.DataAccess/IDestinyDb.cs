using Atheon.DataAccess.Models.Administration;
using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.DataAccess.Models.Destiny.Clans;
using Atheon.DataAccess.Models.Destiny.Guilds;
using Atheon.DataAccess.Models.Destiny.Links;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.DataAccess.Models.Destiny.Tracking;
using Atheon.DataAccess.Models.Discord;
using DotNetBungieAPI.Models.Destiny;

namespace Atheon.DataAccess;

public interface IDestinyDb
{
    Task<List<GuildReference>> GetGuildReferencesAsync();
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettings();
    Task<DiscordGuildSettingsDbModel?> GetGuildSettingsAsync(ulong guildId);
    Task UpsertGuildSettingsAsync(DiscordGuildSettingsDbModel guildSettings);
    Task DeleteGuildSettingsAsync(ulong guildId);
    Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettingsForClanAsync(long clanId);
    Task<List<ClanReference>> GetClanReferencesFromGuildAsync(ulong guildId);
    Task<DiscordDestinyLanguageEnum> GetGuildLanguageAsync(ulong guildId);

    Task<List<long>> GetClanIdsAsync(bool isTracking, DateTime oldenThan);
    Task<DestinyClanDbModel?> GetClanModelAsync(long clanId);
    Task UpsertClanModelAsync(DestinyClanDbModel clanDbModel);
    Task<List<ClanMemberReference>> GetClanMemberReferencesAsync(long clanId);
    Task<List<ClanReference>> GetClanReferencesFromIdsAsync(long[] clanIds);
    Task SetClanRescanAsync(long clanId);
    Task SetClanRescanForAllTrackedClansAsync();

    Task<DestinyProfileDbModel?> GetDestinyProfileAsync(long membershipId);
    Task UpsertDestinyProfileAsync(DestinyProfileDbModel profileDbModel);
    Task<string?> GetProfileDisplayNameAsync(long membershipId);
    Task DeleteDestinyProfileAsync(long membershipId);

    Task<List<DestinyProfileLite>> GetProfilesCollectibleStatusAsync(uint collectibleHash, bool hasItem, long[] clandIds);
    Task<List<DestinyProfileLite>> GetProfilesRecordStatusAsync(uint recordHash, bool hasCompletedRecord, long[] clandIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetProfileDrystreaksAsync(uint collectibleHash, long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetProfileTitlesAsync(uint titleRecordHash, bool hasTitle, long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetProfileMetricsAsync(uint metricHash, bool descending, long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetGuardianRanksLeaderboardAsync(long[] clanIds);
    Task<List<DestinyProfileLiteWithDoubleValues<int, int>>> GetGuardianPowerLevelAsync(long[] clanIds);
    Task<List<DestinyProfileLiteWithDoubleValues<int, int>>> GetGuardianTriumphScoreAsync(long[] clanIds);
    Task<List<DestinyProfileLiteWithDoubleValues<int, int>>> GetGuardianSeasonPassLevelsAsync(uint pass, uint prestigePass, long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetTimePlayedLeaderboardAsync(long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetRecordObjectiveLeaderboardAsync(uint recordHash, long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetRecordIntervalObjectiveLeaderboardAsync(uint recordHash, long[] clanIds);
    Task<List<DestinyProfileLiteWithValue<int>>> GetTotalTitlesLeaderboardAsync(long[] clanIds);
    Task<List<DestinyProfileLite>> GetPlayersWithGameVersionAsync(DestinyGameVersions gameVersion, bool hasVersion, long[] clanIds);

    Task TryInsertClanBroadcastAsync(ClanBroadcastDbModel clanBroadcast);
    Task TryInsertProfileBroadcastAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);
    Task TryInsertProfileCustomBroadcastAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast);

    Task MarkClanBroadcastSentAsync(ClanBroadcastDbModel clanBroadcast);
    Task MarkUserBroadcastSentAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);
    Task MarkUserCustomBroadcastSentAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast);

    Task UpsertCuratedRecordDefinitionAsync(CuratedRecord curatedRecord);
    Task UpsertCuratedCollectibleDefinitionAsync(CuratedCollectible curatedCollectible);
    Task ClearAllCuratedTables();

    Task<List<CuratedRecord>> GetCuratedRecordsAsync();
    Task<List<CuratedCollectible>> GetCuratedCollectiblesAsync();

    Task UpsertProfileLinkAsync(DiscordToDestinyProfileLink link);
    Task RemoveProfileLinkAsync(ulong discordUserId);

    Task AddServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator);
    Task RemoveServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator);
    Task<bool> IsServerAdministratorAsync(ulong guildId, ulong userId);
}
