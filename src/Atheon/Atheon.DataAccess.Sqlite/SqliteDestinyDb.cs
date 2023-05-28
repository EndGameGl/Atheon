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

namespace Atheon.DataAccess.Sqlite;

public class SqliteDestinyDb : IDestinyDb
{
    private readonly IDbAccess _dbAccess;

    public SqliteDestinyDb(IDbAccess dbAccess)
    {
        _dbAccess = dbAccess;
    }

    private const string GetGuildReferencesQuery =
            """
            SELECT 
                GuildId,
                GuildName
            FROM Guilds;
            """;
    public async Task<List<GuildReference>> GetGuildReferencesAsync()
    {
        return await _dbAccess.QueryAsync<GuildReference>(GetGuildReferencesQuery);
    }

    private const string GetAllGuildSettingsQuery =
            """
            SELECT * FROM Guilds;
            """;
    public async Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettings()
    {
        return await _dbAccess.QueryAsync<DiscordGuildSettingsDbModel>(GetAllGuildSettingsQuery);
    }

    private const string UpsertGuildSettingsQuery =
    $"""
    INSERT INTO Guilds 
        (
            {nameof(DiscordGuildSettingsDbModel.GuildId)},
            {nameof(DiscordGuildSettingsDbModel.GuildName)},
            {nameof(DiscordGuildSettingsDbModel.DefaultReportChannel)},
            {nameof(DiscordGuildSettingsDbModel.TrackedMetrics)},
            {nameof(DiscordGuildSettingsDbModel.TrackedRecords)},
            {nameof(DiscordGuildSettingsDbModel.TrackedCollectibles)},
            {nameof(DiscordGuildSettingsDbModel.TrackedProgressions)},
            {nameof(DiscordGuildSettingsDbModel.SystemReportsEnabled)},
            {nameof(DiscordGuildSettingsDbModel.SystemReportsOverrideChannel)},
            {nameof(DiscordGuildSettingsDbModel.Clans)},
            {nameof(DiscordGuildSettingsDbModel.ReportClanChanges)},
            {nameof(DiscordGuildSettingsDbModel.DestinyManifestLocale)}
        )
        VALUES 
        (
            @{nameof(DiscordGuildSettingsDbModel.GuildId)},
            @{nameof(DiscordGuildSettingsDbModel.GuildName)},
            @{nameof(DiscordGuildSettingsDbModel.DefaultReportChannel)},
            @{nameof(DiscordGuildSettingsDbModel.TrackedMetrics)},
            @{nameof(DiscordGuildSettingsDbModel.TrackedRecords)},
            @{nameof(DiscordGuildSettingsDbModel.TrackedCollectibles)},
            @{nameof(DiscordGuildSettingsDbModel.TrackedProgressions)},
            @{nameof(DiscordGuildSettingsDbModel.SystemReportsEnabled)},
            @{nameof(DiscordGuildSettingsDbModel.SystemReportsOverrideChannel)},
            @{nameof(DiscordGuildSettingsDbModel.Clans)},
            @{nameof(DiscordGuildSettingsDbModel.ReportClanChanges)},
            @{nameof(DiscordGuildSettingsDbModel.DestinyManifestLocale)}
        )
        ON CONFLICT (GuildId) DO UPDATE SET
            {nameof(DiscordGuildSettingsDbModel.GuildId)} = @{nameof(DiscordGuildSettingsDbModel.GuildId)},
            {nameof(DiscordGuildSettingsDbModel.GuildName)} = @{nameof(DiscordGuildSettingsDbModel.GuildName)},
            {nameof(DiscordGuildSettingsDbModel.DefaultReportChannel)} = @{nameof(DiscordGuildSettingsDbModel.DefaultReportChannel)},
            {nameof(DiscordGuildSettingsDbModel.TrackedMetrics)} = @{nameof(DiscordGuildSettingsDbModel.TrackedMetrics)},
            {nameof(DiscordGuildSettingsDbModel.TrackedRecords)} = @{nameof(DiscordGuildSettingsDbModel.TrackedRecords)},
            {nameof(DiscordGuildSettingsDbModel.TrackedCollectibles)} = @{nameof(DiscordGuildSettingsDbModel.TrackedCollectibles)},
            {nameof(DiscordGuildSettingsDbModel.TrackedProgressions)} = @{nameof(DiscordGuildSettingsDbModel.TrackedProgressions)},
            {nameof(DiscordGuildSettingsDbModel.SystemReportsEnabled)} = @{nameof(DiscordGuildSettingsDbModel.SystemReportsEnabled)},
            {nameof(DiscordGuildSettingsDbModel.SystemReportsOverrideChannel)} = @{nameof(DiscordGuildSettingsDbModel.SystemReportsOverrideChannel)},
            {nameof(DiscordGuildSettingsDbModel.Clans)} = @{nameof(DiscordGuildSettingsDbModel.Clans)},
            {nameof(DiscordGuildSettingsDbModel.ReportClanChanges)} = @{nameof(DiscordGuildSettingsDbModel.ReportClanChanges)},
            {nameof(DiscordGuildSettingsDbModel.DestinyManifestLocale)} = @{nameof(DiscordGuildSettingsDbModel.DestinyManifestLocale)}
    """;
    public async Task UpsertGuildSettingsAsync(DiscordGuildSettingsDbModel guildSettings)
    {
        await _dbAccess.ExecuteAsync(UpsertGuildSettingsQuery, guildSettings);
    }

    private const string DeleteGuildSettingsQuery =
    $"""
    DELETE FROM Guilds
    WHERE {nameof(DiscordGuildSettingsDbModel.GuildId)} = @{nameof(DiscordGuildSettingsDbModel.GuildId)};
    """;
    public async Task DeleteGuildSettingsAsync(ulong guildId)
    {
        await _dbAccess.ExecuteAsync(DeleteGuildSettingsQuery, new { GuildId = guildId });
    }

    private const string GetGuildSettingsQuery =
            """
            SELECT * FROM Guilds WHERE GuildId = @GuildId;
            """;
    public async Task<DiscordGuildSettingsDbModel?> GetGuildSettingsAsync(ulong guildId)
    {
        return await _dbAccess.QueryFirstOrDefaultAsync<DiscordGuildSettingsDbModel?>(GetGuildSettingsQuery, new { GuildId = guildId });
    }

    private const string GetAllGuildSettingsForClanQuery =
        """
        SELECT * FROM Guilds 
        WHERE EXISTS (SELECT 1 FROM json_each(Clans) WHERE value = @ClanId);
        """;
    public async Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettingsForClanAsync(long clanId)
    {
        return await _dbAccess.QueryAsync<DiscordGuildSettingsDbModel>(GetAllGuildSettingsForClanQuery, new { ClanId = clanId });
    }

    private const string GetClanIdsQuery =
        $"""
        SELECT ({nameof(DestinyClanDbModel.ClanId)}) FROM Clans 
            WHERE 
                {nameof(DestinyClanDbModel.IsTracking)} = @{nameof(DestinyClanDbModel.IsTracking)} AND
                {nameof(DestinyClanDbModel.LastScan)} < @OlderThan;
        """;
    public async Task<List<long>> GetClanIdsAsync(bool isTracking, DateTime olderThan)
    {
        return await _dbAccess.QueryAsync<long>(
            GetClanIdsQuery,
            new
            {
                IsTracking = isTracking,
                OlderThan = olderThan
            });
    }

    private const string GetClanModelQuery =
        $"""
        SELECT * FROM Clans WHERE {nameof(DestinyClanDbModel.ClanId)} = @{nameof(DestinyClanDbModel.ClanId)}
        """;
    public async Task<DestinyClanDbModel?> GetClanModelAsync(long clanId)
    {
        return await _dbAccess.QueryFirstOrDefaultAsync<DestinyClanDbModel>(GetClanModelQuery, new { ClanId = clanId });
    }

    private const string UpsertClanModelQuery =
        $"""
        INSERT INTO Clans
            (
                {nameof(DestinyClanDbModel.ClanId)},
                {nameof(DestinyClanDbModel.ClanName)},
                {nameof(DestinyClanDbModel.ClanCallsign)},
                {nameof(DestinyClanDbModel.ClanLevel)},
                {nameof(DestinyClanDbModel.MemberCount)},
                {nameof(DestinyClanDbModel.MembersOnline)},
                {nameof(DestinyClanDbModel.IsTracking)},
                {nameof(DestinyClanDbModel.JoinedOn)},
                {nameof(DestinyClanDbModel.LastScan)},
                {nameof(DestinyClanDbModel.ShouldRescan)}
            )
            VALUES 
            (   
                @{nameof(DestinyClanDbModel.ClanId)},
                @{nameof(DestinyClanDbModel.ClanName)},
                @{nameof(DestinyClanDbModel.ClanCallsign)},
                @{nameof(DestinyClanDbModel.ClanLevel)},
                @{nameof(DestinyClanDbModel.MemberCount)},
                @{nameof(DestinyClanDbModel.MembersOnline)},
                @{nameof(DestinyClanDbModel.IsTracking)},
                @{nameof(DestinyClanDbModel.JoinedOn)},
                @{nameof(DestinyClanDbModel.LastScan)},
                @{nameof(DestinyClanDbModel.ShouldRescan)}
            )
            ON CONFLICT ({nameof(DestinyClanDbModel.ClanId)}) DO UPDATE SET 
                {nameof(DestinyClanDbModel.ClanName)} = @{nameof(DestinyClanDbModel.ClanName)},
                {nameof(DestinyClanDbModel.ClanCallsign)} = @{nameof(DestinyClanDbModel.ClanCallsign)},
                {nameof(DestinyClanDbModel.ClanLevel)} = @{nameof(DestinyClanDbModel.ClanLevel)},
                {nameof(DestinyClanDbModel.MemberCount)} = @{nameof(DestinyClanDbModel.MemberCount)},
                {nameof(DestinyClanDbModel.MembersOnline)} = @{nameof(DestinyClanDbModel.MembersOnline)},
                {nameof(DestinyClanDbModel.IsTracking)} = @{nameof(DestinyClanDbModel.IsTracking)},
                {nameof(DestinyClanDbModel.JoinedOn)} = @{nameof(DestinyClanDbModel.JoinedOn)},
                {nameof(DestinyClanDbModel.LastScan)} = @{nameof(DestinyClanDbModel.LastScan)},
                {nameof(DestinyClanDbModel.ShouldRescan)} = @{nameof(DestinyClanDbModel.ShouldRescan)}
        """;
    public async Task UpsertClanModelAsync(DestinyClanDbModel clanDbModel)
    {
        await _dbAccess.ExecuteAsync(UpsertClanModelQuery, clanDbModel);
    }


    private const string GetDestinyProfileQuery =
        $"""
        SELECT * FROM DestinyProfiles WHERE ({nameof(DestinyProfileDbModel.MembershipId)}) = @{nameof(DestinyProfileDbModel.MembershipId)}
        """;
    public async Task<DestinyProfileDbModel?> GetDestinyProfileAsync(long membershipId)
    {
        return await _dbAccess.QueryFirstOrDefaultAsync<DestinyProfileDbModel?>(GetDestinyProfileQuery, new { MembershipId = membershipId });
    }

    private const string UpsertDestinyProfileQuery =
        $"""
        INSERT INTO DestinyProfiles
        (
            {nameof(DestinyProfileDbModel.MembershipId)},
            {nameof(DestinyProfileDbModel.MembershipType)},
            {nameof(DestinyProfileDbModel.Name)},
            {nameof(DestinyProfileDbModel.ClanId)},
            {nameof(DestinyProfileDbModel.DateLastPlayed)},
            {nameof(DestinyProfileDbModel.MinutesPlayedTotal)},
            {nameof(DestinyProfileDbModel.Collectibles)},
            {nameof(DestinyProfileDbModel.Records)},
            {nameof(DestinyProfileDbModel.Progressions)},
            {nameof(DestinyProfileDbModel.ResponseMintedTimestamp)},
            {nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)},
            {nameof(DestinyProfileDbModel.LastUpdated)},
            {nameof(DestinyProfileDbModel.ComputedData)},
            {nameof(DestinyProfileDbModel.Metrics)},
            {nameof(DestinyProfileDbModel.CurrentGuardianRank)},
            {nameof(DestinyProfileDbModel.CurrentActivityData)},
            {nameof(DestinyProfileDbModel.GameVersionsOwned)}
        )
        VALUES 
        (
            @{nameof(DestinyProfileDbModel.MembershipId)},
            @{nameof(DestinyProfileDbModel.MembershipType)},
            @{nameof(DestinyProfileDbModel.Name)},
            @{nameof(DestinyProfileDbModel.ClanId)},
            @{nameof(DestinyProfileDbModel.DateLastPlayed)},
            @{nameof(DestinyProfileDbModel.MinutesPlayedTotal)},
            @{nameof(DestinyProfileDbModel.Collectibles)},
            @{nameof(DestinyProfileDbModel.Records)},
            @{nameof(DestinyProfileDbModel.Progressions)},
            @{nameof(DestinyProfileDbModel.ResponseMintedTimestamp)},
            @{nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)},
            @{nameof(DestinyProfileDbModel.LastUpdated)},
            @{nameof(DestinyProfileDbModel.ComputedData)},
            @{nameof(DestinyProfileDbModel.Metrics)},
            @{nameof(DestinyProfileDbModel.CurrentGuardianRank)},
            @{nameof(DestinyProfileDbModel.CurrentActivityData)},
            @{nameof(DestinyProfileDbModel.GameVersionsOwned)}
        )
        ON CONFLICT ({nameof(DestinyProfileDbModel.MembershipId)}) DO UPDATE SET 
            {nameof(DestinyProfileDbModel.MembershipType)} = @{nameof(DestinyProfileDbModel.MembershipType)},
            {nameof(DestinyProfileDbModel.ClanId)} = @{nameof(DestinyProfileDbModel.ClanId)},
            {nameof(DestinyProfileDbModel.Name)} = @{nameof(DestinyProfileDbModel.Name)},
            {nameof(DestinyProfileDbModel.DateLastPlayed)} = @{nameof(DestinyProfileDbModel.DateLastPlayed)},
            {nameof(DestinyProfileDbModel.MinutesPlayedTotal)} = @{nameof(DestinyProfileDbModel.MinutesPlayedTotal)},
            {nameof(DestinyProfileDbModel.Collectibles)} = @{nameof(DestinyProfileDbModel.Collectibles)},
            {nameof(DestinyProfileDbModel.Records)} = @{nameof(DestinyProfileDbModel.Records)},
            {nameof(DestinyProfileDbModel.Progressions)} = @{nameof(DestinyProfileDbModel.Progressions)},
            {nameof(DestinyProfileDbModel.ResponseMintedTimestamp)} = @{nameof(DestinyProfileDbModel.ResponseMintedTimestamp)},
            {nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)} = @{nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)},
            {nameof(DestinyProfileDbModel.LastUpdated)} = @{nameof(DestinyProfileDbModel.LastUpdated)},
            {nameof(DestinyProfileDbModel.ComputedData)} = @{nameof(DestinyProfileDbModel.ComputedData)},
            {nameof(DestinyProfileDbModel.Metrics)} = @{nameof(DestinyProfileDbModel.Metrics)},
            {nameof(DestinyProfileDbModel.CurrentGuardianRank)} = @{nameof(DestinyProfileDbModel.CurrentGuardianRank)},
            {nameof(DestinyProfileDbModel.CurrentActivityData)} = @{nameof(DestinyProfileDbModel.CurrentActivityData)},
            {nameof(DestinyProfileDbModel.GameVersionsOwned)} = @{nameof(DestinyProfileDbModel.GameVersionsOwned)};
        """;
    public async Task UpsertDestinyProfileAsync(DestinyProfileDbModel profileDbModel)
    {
        await _dbAccess.ExecuteAsync(UpsertDestinyProfileQuery, profileDbModel, default);
    }

    private const string GetProfilesWithCollectibleQuery =
        $"""
        SELECT 
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE EXISTS (SELECT 1 FROM json_each(Collectibles) WHERE value = @CollectibleHash) AND ClanId IN @ClanIds
        """;

    private const string GetProfilesWithoutCollectibleQuery =
        $"""
        SELECT 
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE NOT EXISTS (SELECT 1 FROM json_each(Collectibles) WHERE value = @CollectibleHash) AND ClanId IN @ClanIds
        """;
    public async Task<List<DestinyProfileLite>> GetProfilesCollectibleStatusAsync(uint collectibleHash, bool hasItem, long[] clandIds)
    {
        if (hasItem)
        {
            return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesWithCollectibleQuery, new
            {
                CollectibleHash = collectibleHash,
                ClanIds = clandIds
            });
        }

        return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesWithoutCollectibleQuery, new
        {
            CollectibleHash = collectibleHash,
            ClanIds = clandIds
        });
    }

    private const string TryInsertClanBroadcastQuery =
        $"""
        INSERT INTO DestinyClanBroadcasts
            (
                {nameof(ClanBroadcastDbModel.GuildId)},
                {nameof(ClanBroadcastDbModel.ClanId)},
                {nameof(ClanBroadcastDbModel.WasAnnounced)},
                {nameof(ClanBroadcastDbModel.Date)},
                {nameof(ClanBroadcastDbModel.Type)},
                {nameof(ClanBroadcastDbModel.OldValue)},
                {nameof(ClanBroadcastDbModel.NewValue)}
            )
            VALUES 
            (
                @{nameof(ClanBroadcastDbModel.GuildId)},
                @{nameof(ClanBroadcastDbModel.ClanId)},
                @{nameof(ClanBroadcastDbModel.WasAnnounced)},
                @{nameof(ClanBroadcastDbModel.Date)},
                @{nameof(ClanBroadcastDbModel.Type)},
                @{nameof(ClanBroadcastDbModel.OldValue)},
                @{nameof(ClanBroadcastDbModel.NewValue)}
            )
        """;
    public async Task TryInsertClanBroadcastAsync(ClanBroadcastDbModel clanBroadcast)
    {
        await _dbAccess.ExecuteAsync(TryInsertClanBroadcastQuery, clanBroadcast);
    }

    private const string TryInsertProfileBroadcastQuery =
        $"""
        INSERT INTO DestinyUserBroadcasts
            (
                {nameof(DestinyUserProfileBroadcastDbModel.GuildId)},
                {nameof(DestinyUserProfileBroadcastDbModel.ClanId)},
                {nameof(DestinyUserProfileBroadcastDbModel.WasAnnounced)},
                {nameof(DestinyUserProfileBroadcastDbModel.Date)},
                {nameof(DestinyUserProfileBroadcastDbModel.Type)},
                {nameof(DestinyUserProfileBroadcastDbModel.MembershipId)},
                {nameof(DestinyUserProfileBroadcastDbModel.DefinitionHash)},
                {nameof(DestinyUserProfileBroadcastDbModel.AdditionalData)}
            )
            VALUES 
            (
                @{nameof(DestinyUserProfileBroadcastDbModel.GuildId)},
                @{nameof(DestinyUserProfileBroadcastDbModel.ClanId)},
                @{nameof(DestinyUserProfileBroadcastDbModel.WasAnnounced)},
                @{nameof(DestinyUserProfileBroadcastDbModel.Date)},
                @{nameof(DestinyUserProfileBroadcastDbModel.Type)},
                @{nameof(DestinyUserProfileBroadcastDbModel.MembershipId)},
                @{nameof(DestinyUserProfileBroadcastDbModel.DefinitionHash)},
                @{nameof(DestinyUserProfileBroadcastDbModel.AdditionalData)}
            )
            ON CONFLICT DO NOTHING;
        """;
    public async Task TryInsertProfileBroadcastAsync(DestinyUserProfileBroadcastDbModel profileBroadcast)
    {
        await _dbAccess.ExecuteAsync(TryInsertProfileBroadcastQuery, profileBroadcast);
    }

    private const string GetProfileDisplayNameQuery =
        $"""
        SELECT Name FROM DestinyProfiles WHERE MembershipId = @MembershipId
        """;
    public async Task<string?> GetProfileDisplayNameAsync(long membershipId)
    {
        return await _dbAccess.QueryFirstOrDefaultAsync<string?>(GetProfileDisplayNameQuery, new { MembershipId = membershipId });
    }


    private const string MarkClanBroadcastSentQuery =
        $"""
        UPDATE DestinyClanBroadcasts
            SET 
                {nameof(ClanBroadcastDbModel.WasAnnounced)} = true
            WHERE 
                {nameof(ClanBroadcastDbModel.Type)} = @{nameof(ClanBroadcastDbModel.Type)} AND
                {nameof(ClanBroadcastDbModel.ClanId)} = @{nameof(ClanBroadcastDbModel.ClanId)} AND
                {nameof(ClanBroadcastDbModel.GuildId)} = @{nameof(ClanBroadcastDbModel.GuildId)} AND
                {nameof(ClanBroadcastDbModel.Date)} = @{nameof(ClanBroadcastDbModel.Date)} AND
                {nameof(ClanBroadcastDbModel.NewValue)} = @{nameof(ClanBroadcastDbModel.NewValue)};
        """;
    public async Task MarkClanBroadcastSentAsync(ClanBroadcastDbModel clanBroadcast)
    {
        await _dbAccess.ExecuteAsync(MarkClanBroadcastSentQuery, clanBroadcast);
    }

    private const string MarkUserBroadcastSentQuery =
        $"""
        UPDATE DestinyUserBroadcasts
            SET 
                {nameof(DestinyUserProfileBroadcastDbModel.WasAnnounced)} = true
            WHERE 
                {nameof(DestinyUserProfileBroadcastDbModel.Type)} = @{nameof(DestinyUserProfileBroadcastDbModel.Type)} AND
                {nameof(DestinyUserProfileBroadcastDbModel.ClanId)} = @{nameof(DestinyUserProfileBroadcastDbModel.ClanId)} AND
                {nameof(DestinyUserProfileBroadcastDbModel.GuildId)} = @{nameof(DestinyUserProfileBroadcastDbModel.GuildId)} AND
                {nameof(DestinyUserProfileBroadcastDbModel.DefinitionHash)} = @{nameof(DestinyUserProfileBroadcastDbModel.DefinitionHash)} AND
                {nameof(DestinyUserProfileBroadcastDbModel.MembershipId)} = @{nameof(DestinyUserProfileBroadcastDbModel.MembershipId)};
        """;
    public async Task MarkUserBroadcastSentAsync(DestinyUserProfileBroadcastDbModel profileBroadcast)
    {
        await _dbAccess.ExecuteAsync(MarkUserBroadcastSentQuery, profileBroadcast);
    }

    private const string GetClanMemberReferencesQuery =
        """
            SELECT MembershipId 
            FROM DestinyProfiles
            WHERE ClanId = @ClanId;
            """;
    public async Task<List<ClanMemberReference>> GetClanMemberReferencesAsync(long clanId)
    {
        return await _dbAccess.QueryAsync<ClanMemberReference>(GetClanMemberReferencesQuery, new { ClanId = clanId });
    }

    private const string DeleteDestinyProfileQuery =
        $"""
        DELETE FROM DestinyProfiles WHERE {nameof(DestinyProfileDbModel.MembershipId)} = @MembershipId;
        """;
    public async Task DeleteDestinyProfileAsync(long membershipId)
    {
        await _dbAccess.ExecuteAsync(DeleteDestinyProfileQuery, new { MembershipId = membershipId });
    }

    private const string GetProfilesRecordStatusCompletedQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE EXISTS (SELECT 1 FROM json_each(Records) WHERE CAST(key as INTEGER) = @RecordHash AND json_extract(value, '$.state') NOT IN (4)) AND ClanId IN @ClanIds
        """;

    private const string GetProfilesRecordStatusNotCompletedQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE EXISTS (SELECT 1 FROM json_each(Records) WHERE CAST(key as INTEGER) = @RecordHash AND json_extract(value, '$.state') IN (4)) AND ClanId IN @ClanIds
        """;
    public async Task<List<DestinyProfileLite>> GetProfilesRecordStatusAsync(uint recordHash, bool hasCompletedRecord, long[] clanIds)
    {
        if (hasCompletedRecord)
        {
            return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesRecordStatusCompletedQuery, new
            {
                RecordHash = recordHash,
                ClanIds = clanIds
            });
        }
        return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesRecordStatusNotCompletedQuery, new
        {
            RecordHash = recordHash,
            ClanIds = clanIds
        });
    }

    private const string UpsertCuratedRecordDefinitionQuery =
        $"""
        INSERT INTO CuratedRecords
            (
                {nameof(CuratedRecord.Hash)},
                {nameof(CuratedRecord.IsEnabled)},
                {nameof(CuratedRecord.OverrideName)}
            )
            VALUES 
            (
                @{nameof(CuratedRecord.Hash)},
                @{nameof(CuratedRecord.IsEnabled)},
                @{nameof(CuratedRecord.OverrideName)}
            )
            ON CONFLICT ({nameof(CuratedRecord.Hash)}) DO UPDATE SET 
                {nameof(CuratedRecord.IsEnabled)} = @{nameof(CuratedRecord.IsEnabled)},
                {nameof(CuratedRecord.OverrideName)} = @{nameof(CuratedRecord.OverrideName)}
        """;
    public async Task UpsertCuratedRecordDefinitionAsync(CuratedRecord curatedRecord)
    {
        await _dbAccess.ExecuteAsync(UpsertCuratedRecordDefinitionQuery, curatedRecord);
    }

    private const string UpsertCuratedCollectibleDefinitionQuery =
        $"""
        INSERT INTO CuratedCollectibles
            (
                {nameof(CuratedCollectible.Hash)},
                {nameof(CuratedCollectible.IsEnabled)},
                {nameof(CuratedCollectible.OverrideName)}
            )
            VALUES 
            (
                @{nameof(CuratedCollectible.Hash)},
                @{nameof(CuratedCollectible.IsEnabled)},
                @{nameof(CuratedCollectible.OverrideName)}
            )
            ON CONFLICT ({nameof(CuratedCollectible.Hash)}) DO UPDATE SET 
                {nameof(CuratedCollectible.IsEnabled)} = @{nameof(CuratedCollectible.IsEnabled)},
                {nameof(CuratedCollectible.OverrideName)} = @{nameof(CuratedCollectible.OverrideName)}
        """;
    public async Task UpsertCuratedCollectibleDefinitionAsync(CuratedCollectible curatedCollectible)
    {
        await _dbAccess.ExecuteAsync(UpsertCuratedCollectibleDefinitionQuery, curatedCollectible);
    }

    private const string GetCuratedRecordsQuery =
        $"""
        SELECT * FROM CuratedRecords;
        """;
    public async Task<List<CuratedRecord>> GetCuratedRecordsAsync()
    {
        return await _dbAccess.QueryAsync<CuratedRecord>(GetCuratedRecordsQuery);
    }

    private const string GetCuratedCollectiblesQuery =
        $"""
        SELECT * FROM CuratedCollectibles;
        """;
    public async Task<List<CuratedCollectible>> GetCuratedCollectiblesAsync()
    {
        return await _dbAccess.QueryAsync<CuratedCollectible>(GetCuratedCollectiblesQuery);
    }

    private const string UpsertProfileLinkQuery =
        $"""
        INSERT INTO DiscordToDestinyProfileLinks
        (
            {nameof(DiscordToDestinyProfileLink.DiscordUserId)},
            {nameof(DiscordToDestinyProfileLink.DestinyMembershipId)},
            {nameof(DiscordToDestinyProfileLink.BungieMembershipType)}
        )
        VALUES 
        (
            @{nameof(DiscordToDestinyProfileLink.DiscordUserId)},
            @{nameof(DiscordToDestinyProfileLink.DestinyMembershipId)},
            @{nameof(DiscordToDestinyProfileLink.BungieMembershipType)}
        )
        ON CONFLICT ({nameof(DiscordToDestinyProfileLink.DiscordUserId)}) DO UPDATE SET
            {nameof(DiscordToDestinyProfileLink.DestinyMembershipId)} = @{nameof(DiscordToDestinyProfileLink.DestinyMembershipId)},
            {nameof(DiscordToDestinyProfileLink.BungieMembershipType)} = @{nameof(DiscordToDestinyProfileLink.BungieMembershipType)}
        """;
    public async Task UpsertProfileLinkAsync(DiscordToDestinyProfileLink link)
    {
        await _dbAccess.ExecuteAsync(UpsertProfileLinkQuery, link);
    }

    private const string RemoveProfileLinkQuery =
        """
        DELETE FROM DiscordToDestinyProfileLinks WHERE DiscordUserId = @DiscordUserId;
        """;
    public async Task RemoveProfileLinkAsync(ulong discordUserId)
    {
        await _dbAccess.ExecuteAsync(RemoveProfileLinkQuery, new { DiscordUserId = discordUserId });
    }

    private const string GetClanReferencesFromGuildQuery =
        """
        SELECT 
        	value as Id,
        	Clans.ClanName as Name
        FROM 
        	Guilds, 
        	json_each(Guilds.Clans)
        LEFT JOIN Clans ON Clans.ClanId = value
        WHERE GuildId = @GuildId
        """;
    public async Task<List<ClanReference>> GetClanReferencesFromGuildAsync(ulong guildId)
    {
        return await _dbAccess.QueryAsync<ClanReference>(GetClanReferencesFromGuildQuery, new { GuildId = guildId });
    }

    private const string AddServerAdministratorQuery =
        $"""
        INSERT INTO ServerBotAdministrators
        (
            {nameof(ServerBotAdministrator.DiscordGuildId)},
            {nameof(ServerBotAdministrator.DiscordUserId)}
        )
        VALUES 
        (
            @{nameof(ServerBotAdministrator.DiscordGuildId)},
            @{nameof(ServerBotAdministrator.DiscordUserId)}
        )
        ON CONFLICT (DiscordGuildId, DiscordUserId) DO NOTHING
        """;
    public async Task AddServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator)
    {
        await _dbAccess.ExecuteAsync(AddServerAdministratorQuery, serverBotAdministrator);
    }

    private const string RemoveServerAdministratorQuery =
        $"""
        DELETE FROM ServerBotAdministrators
        WHERE 
            {nameof(ServerBotAdministrator.DiscordGuildId)} = @{nameof(ServerBotAdministrator.DiscordGuildId)} AND
            {nameof(ServerBotAdministrator.DiscordUserId)} = @{nameof(ServerBotAdministrator.DiscordUserId)}
        """;
    public async Task RemoveServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator)
    {
        await _dbAccess.ExecuteAsync(RemoveServerAdministratorQuery, serverBotAdministrator);
    }

    private const string IsServerAdministratorQuery =
        $"""
        SELECT 1 FROM ServerBotAdministrators
        WHERE 
            {nameof(ServerBotAdministrator.DiscordGuildId)} = @{nameof(ServerBotAdministrator.DiscordGuildId)} AND
            {nameof(ServerBotAdministrator.DiscordUserId)} = @{nameof(ServerBotAdministrator.DiscordUserId)}
        """;
    public async Task<bool> IsServerAdministratorAsync(ulong guildId, ulong userId)
    {
        var result = await _dbAccess.QueryFirstOrDefaultAsync<bool?>(IsServerAdministratorQuery,
            new
            {
                DiscordGuildId = guildId,
                DiscordUserId = userId
            });
        if (!result.HasValue)
            return false;
        return result.Value;
    }

    private const string GetClansFromIdsQuery =
        """
        SELECT 
            ClanId as Id,
            ClanName as Name
        FROM Clans
        WHERE ClanId IN @ClanIds;
        """;
    public async Task<List<ClanReference>> GetClanReferencesFromIdsAsync(long[] clanIds)
    {
        return await _dbAccess.QueryAsync<ClanReference>(GetClansFromIdsQuery, new { ClanIds = clanIds });
    }

    private const string GetProfileDrystreaksQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId,
            json_extract(ComputedData, '$.drystreaks.{0}') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value IS NOT NULL AND Value != 0
        ORDER BY Value DESC
        """;

    public async Task<List<DestinyProfileLiteWithValue<int>>> GetProfileDrystreaksAsync(uint collectibleHash, long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
            string.Format(GetProfileDrystreaksQuery, collectibleHash),
            new
            {
                ClanIds = clanIds
            });
    }

    private const string GetProfilesWithTitleQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId,
            json_extract(ComputedData, '$.titles.{0}') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value IS NOT NULL AND Value != 0
        ORDER BY Value DESC
        """;

    private const string GetProfilesWithoutTitleQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId,
            json_extract(ComputedData, '$.titles.{0}') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value IS NULL OR Value = 0
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetProfileTitlesAsync(uint titleRecordHash, bool hasTitle, long[] clanIds)
    {
        if (hasTitle)
        {
            return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                string.Format(GetProfilesWithTitleQuery, titleRecordHash),
                new { ClanIds = clanIds });
        }

        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                string.Format(GetProfilesWithoutTitleQuery, titleRecordHash),
                new { ClanIds = clanIds });
    }

    private const string DeleteCuratedCollectiblesQuery =
        """
        DELETE FROM CuratedCollectibles;
        """;
    public async Task ClearAllCuratedTables()
    {
        await _dbAccess.ExecuteAsync(DeleteCuratedCollectiblesQuery);
    }


    private const string GetProfileMetricsQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId,
            json_extract(Metrics, '$.{0}.progress.progress') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value != 0
        ORDER BY Value {1};
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetProfileMetricsAsync(
        uint metricHash,
        bool descending,
        long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                string.Format(GetProfileMetricsQuery, metricHash, descending ? "DESC" : string.Empty),
                new { ClanIds = clanIds });
    }

    private const string GetGuardianRanksLeaderboard =
        $"""
        SELECT
            MembershipId,
            Name,
            ClanId,
            {nameof(DestinyProfileDbModel.CurrentGuardianRank)} as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value > 0
        ORDER BY Value DESC
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetGuardianRanksLeaderboardAsync(long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                GetGuardianRanksLeaderboard,
                new { ClanIds = clanIds });
    }

    private const string GetGuardianPowerLevelQuery =
        $"""
        SELECT
            MembershipId,
            Name,
            ClanId,
            json_extract(ComputedData, '$.powerLevel') as FirstValue,
            json_extract(ComputedData, '$.artifactPowerLevel') as SecondValue
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND (FirstValue > 0 OR SecondValue > 0)
        ORDER BY (FirstValue + SecondValue) DESC
        """;
    public async Task<List<DestinyProfileLiteWithDoubleValues<int, int>>> GetGuardianPowerLevelAsync(long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithDoubleValues<int, int>>(
                 GetGuardianPowerLevelQuery,
                 new { ClanIds = clanIds });
    }

    private const string GetGuardianTriumphScoreQuery =
        $"""
        SELECT
            MembershipId,
            Name,
            ClanId,
            json_extract(ComputedData, '$.activeScore') as FirstValue,
            json_extract(ComputedData, '$.legacyScore') as SecondValue
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND (FirstValue > 0 OR SecondValue > 0) 
        ORDER BY (FirstValue + SecondValue) DESC
        """;
    public async Task<List<DestinyProfileLiteWithDoubleValues<int, int>>> GetGuardianTriumphScoreAsync(long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithDoubleValues<int, int>>(
                 GetGuardianTriumphScoreQuery,
                 new { ClanIds = clanIds });
    }

    private const string GetTimePlayedLeaderboardQuery =
        """
        SELECT
            MembershipId,
            Name,
            ClanId,
            MinutesPlayedTotal as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value > 0
        ORDER BY Value DESC
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetTimePlayedLeaderboardAsync(long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                GetTimePlayedLeaderboardQuery,
                new { ClanIds = clanIds });
    }

    private const string GetRecordObjectiveLeaderboardQuery =
        """
        SELECT
            MembershipId,
            Name,
            ClanId,
            json_extract(Records, '$.{0}.objectives[#-1].progress') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value > 0
        ORDER BY Value DESC
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetRecordObjectiveLeaderboardAsync(uint recordHash, long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                string.Format(GetRecordObjectiveLeaderboardQuery, recordHash),
                new { ClanIds = clanIds });
    }


    private const string GetRecordIntervalObjectiveLeaderboardQuery =
        """
        SELECT
            MembershipId,
            Name,
            ClanId,
            json_extract(Records, '$.{0}.intervalObjectives[#-1].progress') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value > 0
        ORDER BY Value DESC
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetRecordIntervalObjectiveLeaderboardAsync(uint recordHash, long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                string.Format(GetRecordIntervalObjectiveLeaderboardQuery, recordHash),
                new { ClanIds = clanIds });
    }

    private const string GetTotalTitlesLeaderboardQuery =
        """
        SELECT
            MembershipId,
            Name,
            ClanId,
            json_extract(ComputedData, '$.totalTitlesEarned') as Value
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND Value > 0
        ORDER BY Value DESC
        """;
    public async Task<List<DestinyProfileLiteWithValue<int>>> GetTotalTitlesLeaderboardAsync(long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithValue<int>>(
                GetTotalTitlesLeaderboardQuery,
                new { ClanIds = clanIds });
    }

    private const string GetGuildLanguageQuery =
        """
        SELECT DestinyManifestLocale
        FROM Guilds
        WHERE GuildId = @GuildId
        """;
    public async Task<DiscordDestinyLanguageEnum> GetGuildLanguageAsync(ulong guildId)
    {
        var result = await _dbAccess.QueryFirstOrDefaultAsync<DiscordDestinyLanguageEnum?>(GetGuildLanguageQuery, new
        {
            GuildId = guildId
        });

        if (result is null)
            return DiscordDestinyLanguageEnum.English;

        return result.Value;
    }

    private const string SetClanRescanQuery =
        $"""
        UPDATE Clans SET {nameof(DestinyClanDbModel.ShouldRescan)} = 1 WHERE {nameof(DestinyClanDbModel.ClanId)} = @ClanId;
        """;
    public async Task SetClanRescanAsync(long clanId)
    {
        await _dbAccess.ExecuteAsync(SetClanRescanQuery, new { ClanId = clanId });
    }


    private const string SetClanRescanForAllTrackedClansQuery =
        $"""
        UPDATE Clans SET {nameof(DestinyClanDbModel.ShouldRescan)} = 1 WHERE {nameof(DestinyClanDbModel.IsTracking)} = 1;
        """;
    public async Task SetClanRescanForAllTrackedClansAsync()
    {
        await _dbAccess.ExecuteAsync(SetClanRescanForAllTrackedClansQuery);
    }

    private const string GetPlayersWithoutGameVersionQuery =
        $"""
        SELECT
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND GameVersionsOwned & (@Version) == 0
        """;

    private const string GetPlayersWithGameVersionQuery =
        $"""
        SELECT
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND GameVersionsOwned & (@Version) != 0
        """;
    public async Task<List<DestinyProfileLite>> GetPlayersWithGameVersionAsync(
        DestinyGameVersions gameVersion,
        bool hasVersion,
        long[] clanIds)
    {
        if (hasVersion)
        {
            return await _dbAccess.QueryAsync<DestinyProfileLite>(GetPlayersWithGameVersionQuery,
                new
                {
                    ClanIds = clanIds,
                    Version = gameVersion
                });
        }

        return await _dbAccess.QueryAsync<DestinyProfileLite>(GetPlayersWithoutGameVersionQuery,
            new
            {
                ClanIds = clanIds,
                Version = gameVersion
            });
    }

    private const string TryInsertProfileCustomBroadcastQuery =
        $"""
        INSERT INTO DestinyUserCustomBroadcasts
            (
                {nameof(DestinyUserProfileCustomBroadcastDbModel.GuildId)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.ClanId)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.WasAnnounced)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.Date)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.Type)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.MembershipId)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.OldValue)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.NewValue)},
                {nameof(DestinyUserProfileCustomBroadcastDbModel.AdditionalData)}
            )
            VALUES 
            (
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.GuildId)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.ClanId)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.WasAnnounced)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.Date)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.Type)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.MembershipId)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.OldValue)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.NewValue)},
                @{nameof(DestinyUserProfileCustomBroadcastDbModel.AdditionalData)}
            )
            ON CONFLICT DO NOTHING;
        """;

    public async Task TryInsertProfileCustomBroadcastAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast)
    {
        await _dbAccess.ExecuteAsync(TryInsertProfileCustomBroadcastQuery, profileCustomBroadcast);
    }

    private const string MarkUserCustomBroadcastSentQuery =
        $"""
        UPDATE DestinyUserCustomBroadcasts
            SET 
                {nameof(DestinyUserProfileCustomBroadcastDbModel.WasAnnounced)} = true
            WHERE 
                {nameof(DestinyUserProfileCustomBroadcastDbModel.Type)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.Type)} AND
                {nameof(DestinyUserProfileCustomBroadcastDbModel.ClanId)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.ClanId)} AND
                {nameof(DestinyUserProfileCustomBroadcastDbModel.GuildId)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.GuildId)} AND
                {nameof(DestinyUserProfileCustomBroadcastDbModel.OldValue)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.OldValue)} AND
                {nameof(DestinyUserProfileCustomBroadcastDbModel.OldValue)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.OldValue)} AND
                {nameof(DestinyUserProfileCustomBroadcastDbModel.MembershipId)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.MembershipId)};
        """;

    public async Task MarkUserCustomBroadcastSentAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast)
    {
        await _dbAccess.ExecuteAsync(MarkUserCustomBroadcastSentQuery, profileCustomBroadcast);
    }

    private const string GetGuardianSeasonPassLevelsQuery =
       """
        SELECT
            MembershipId,
            Name,
            ClanId,
            json_extract(Progressions, '$.{0}.level') as FirstValue,
            json_extract(Progressions, '$.{1}.level') as SecondValue
        FROM DestinyProfiles
        WHERE ClanId IN @ClanIds AND (FirstValue > 0 OR SecondValue > 0) 
        ORDER BY (FirstValue + SecondValue) DESC
        """;

    public async Task<List<DestinyProfileLiteWithDoubleValues<int, int>>> GetGuardianSeasonPassLevelsAsync(
        uint pass,
        uint prestigePass,
        long[] clanIds)
    {
        return await _dbAccess.QueryAsync<DestinyProfileLiteWithDoubleValues<int, int>>(
            string.Format(GetGuardianSeasonPassLevelsQuery, pass, prestigePass),
            new
            {
                ClanIds = clanIds
            });
    }
}
