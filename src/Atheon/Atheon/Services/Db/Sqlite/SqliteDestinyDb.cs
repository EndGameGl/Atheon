using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Guilds;
using Atheon.Services.Interfaces;

namespace Atheon.Services.Db.Sqlite
{
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
            SELECT ({nameof(DestinyClanDbModel.ClanId)}) FROM Clans WHERE {nameof(DestinyClanDbModel.IsTracking)} = @{nameof(DestinyClanDbModel.IsTracking)};
            """;
        public async Task<List<long>> GetClanIdsAsync(bool isTracking)
        {
            return await _dbAccess.QueryAsync<long>(GetClanIdsQuery, new { IsTracking = isTracking });
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
                {nameof(DestinyClanDbModel.LastScan)}
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
                @{nameof(DestinyClanDbModel.LastScan)}
            )
            ON CONFLICT ({nameof(DestinyClanDbModel.ClanId)}) DO UPDATE SET 
                {nameof(DestinyClanDbModel.ClanName)} = @{nameof(DestinyClanDbModel.ClanName)},
                {nameof(DestinyClanDbModel.ClanCallsign)} = @{nameof(DestinyClanDbModel.ClanCallsign)},
                {nameof(DestinyClanDbModel.ClanLevel)} = @{nameof(DestinyClanDbModel.ClanLevel)},
                {nameof(DestinyClanDbModel.MemberCount)} = @{nameof(DestinyClanDbModel.MemberCount)},
                {nameof(DestinyClanDbModel.MembersOnline)} = @{nameof(DestinyClanDbModel.MembersOnline)},
                {nameof(DestinyClanDbModel.IsTracking)} = @{nameof(DestinyClanDbModel.IsTracking)},
                {nameof(DestinyClanDbModel.JoinedOn)} = @{nameof(DestinyClanDbModel.JoinedOn)},
                {nameof(DestinyClanDbModel.LastScan)} = @{nameof(DestinyClanDbModel.LastScan)}
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
                {nameof(DestinyProfileDbModel.ClanId)},
                {nameof(DestinyProfileDbModel.Name)},
                {nameof(DestinyProfileDbModel.DateLastPlayed)},
                {nameof(DestinyProfileDbModel.MinutesPlayedTotal)},
                {nameof(DestinyProfileDbModel.Collectibles)},
                {nameof(DestinyProfileDbModel.Records)},
                {nameof(DestinyProfileDbModel.Progressions)},
                {nameof(DestinyProfileDbModel.ResponseMintedTimestamp)},
                {nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)}
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
                @{nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)}
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
                {nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)} = @{nameof(DestinyProfileDbModel.SecondaryComponentsMintedTimestamp)}
            """;
        public async Task UpsertDestinyProfileAsync(DestinyProfileDbModel profileDbModel)
        {
            await _dbAccess.ExecuteAsync(UpsertDestinyProfileQuery, profileDbModel, default);
        }

        private const string GetProfilesWithCollectibleQuery =
            $"""
            SELECT *
            FROM DestinyProfiles
            WHERE EXISTS (SELECT 1 FROM json_each(Collectibles) WHERE value = @CollectibleHash)
            """;
        public async Task<List<DestinyProfileDbModel>> GetProfilesWithCollectibleAsync(uint collectibleHash)
        {
            return await _dbAccess.QueryAsync<DestinyProfileDbModel>(GetProfilesWithCollectibleQuery, new { CollectibleHash = collectibleHash });
        }
    }
}
