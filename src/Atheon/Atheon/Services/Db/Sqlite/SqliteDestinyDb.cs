using Atheon.Models.Database.Destiny;
using Atheon.Services.Interfaces;
using System.Security.Claims;

namespace Atheon.Services.Db.Sqlite
{
    public class SqliteDestinyDb : IDestinyDb
    {
        private readonly IDbAccess _dbAccess;

        public SqliteDestinyDb(IDbAccess dbAccess)
        {
            _dbAccess = dbAccess;
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


        private const string GetDestinyProfileQuery =
            $"""
            SELECT * FROM DestinyProfiles WHERE ({nameof(DestinyProfileDbModel.MembershipId)}) = @{nameof(DestinyProfileDbModel.MembershipId)}
            """;
        public async Task<DestinyProfileDbModel?> GetDestinyProfileAsync(long membershipId)
        {
            return await _dbAccess.QueryFirstOrDefaultAsync<DestinyProfileDbModel?>(GetDestinyProfileQuery, new { MembershipId = membershipId });
        }
    }
}
