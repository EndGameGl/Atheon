using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Clans;
using Atheon.DataAccess.Models.Destiny.Guilds;
using Atheon.DataAccess.Models.Discord;

namespace Atheon.DataAccess.Sqlite;

public class GuildDb : IGuildDb
{
    private readonly IDbAccess _dbAccess;

    public GuildDb(IDbAccess dbAccess)
    {
        _dbAccess = dbAccess;
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

    private const string GetAllGuildSettingsQuery =
            """
            SELECT * FROM Guilds;
            """;
    public async Task<List<DiscordGuildSettingsDbModel>> GetAllGuildSettings()
    {
        return await _dbAccess.QueryAsync<DiscordGuildSettingsDbModel>(GetAllGuildSettingsQuery);
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

    private const string GetGuildSettingsQuery =
            """
            SELECT * FROM Guilds WHERE GuildId = @GuildId;
            """;
    public async Task<DiscordGuildSettingsDbModel?> GetGuildSettingsAsync(ulong guildId)
    {
        return await _dbAccess.QueryFirstOrDefaultAsync<DiscordGuildSettingsDbModel?>(GetGuildSettingsQuery, new { GuildId = guildId });
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
}
