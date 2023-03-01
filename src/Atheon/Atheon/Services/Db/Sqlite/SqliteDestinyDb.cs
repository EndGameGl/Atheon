using Atheon.Models.Database.Administration;
using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Models.Database.Destiny.Clans;
using Atheon.Models.Database.Destiny.Guilds;
using Atheon.Models.Database.Destiny.Links;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Models.Database.Destiny.Tracking;
using Atheon.Services.Interfaces;

namespace Atheon.Services.Db.Sqlite;

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
            {nameof(DiscordGuildSettingsDbModel.ReportClanChanges)}
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
            @{nameof(DiscordGuildSettingsDbModel.ReportClanChanges)}
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
            {nameof(DiscordGuildSettingsDbModel.ReportClanChanges)} = @{nameof(DiscordGuildSettingsDbModel.ReportClanChanges)}
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
                {nameof(DestinyProfileDbModel.LastUpdated)}
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
                @{nameof(DestinyProfileDbModel.LastUpdated)}
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
                {nameof(DestinyProfileDbModel.LastUpdated)} = @{nameof(DestinyProfileDbModel.LastUpdated)}
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
        WHERE EXISTS (SELECT 1 FROM json_each(Collectibles) WHERE value = @CollectibleHash)
        """;

    private const string GetProfilesWithoutCollectibleQuery =
        $"""
        SELECT 
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE NOT EXISTS (SELECT 1 FROM json_each(Collectibles) WHERE value = @CollectibleHash)
        """;
    public async Task<List<DestinyProfileLite>> GetProfilesCollectibleStatusAsync(uint collectibleHash, bool hasItem)
    {
        if (hasItem)
        {
            return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesWithCollectibleQuery, new { CollectibleHash = collectibleHash });
        }

        return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesWithoutCollectibleQuery, new { CollectibleHash = collectibleHash });
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
        WHERE EXISTS (SELECT 1 FROM json_each(Records) WHERE CAST(key as INTEGER) = @RecordHash AND json_extract(value, '$.state') NOT IN (4))
        """;

    private const string GetProfilesRecordStatusNotCompletedQuery =
        """
        SELECT 
            MembershipId,
            Name,
            ClanId
        FROM DestinyProfiles
        WHERE EXISTS (SELECT 1 FROM json_each(Records) WHERE CAST(key as INTEGER) = @RecordHash AND json_extract(value, '$.state') IN (4))
        """;
    public async Task<List<DestinyProfileLite>> GetProfilesRecordStatusAsync(uint recordHash, bool hasCompletedRecord)
    {
        if (hasCompletedRecord)
        {
            return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesRecordStatusCompletedQuery, new { RecordHash = recordHash });
        }
        return await _dbAccess.QueryAsync<DestinyProfileLite>(GetProfilesRecordStatusNotCompletedQuery, new { RecordHash = recordHash });
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
}
