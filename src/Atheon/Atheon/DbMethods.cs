using Atheon.Models.Destiny;
using Atheon.Services.Interfaces;

namespace Atheon;

public static class DbMethods
{

    private const string UpsertGuildSettingsQuery =
        $"""
        INSERT INTO Guilds 
        (
            {nameof(GuildSettings.GuildId)},
            {nameof(GuildSettings.GuildName)},
            {nameof(GuildSettings.DefaultReportChannel)},
            {nameof(GuildSettings.TrackedMetrics)},
            {nameof(GuildSettings.TrackedRecords)},
            {nameof(GuildSettings.TrackedCollectibles)},
            {nameof(GuildSettings.TrackedProgressions)},
            {nameof(GuildSettings.SystemReportsEnabled)},
            {nameof(GuildSettings.SystemReportsOverrideChannel)},
            {nameof(GuildSettings.Clans)}
        )
        VALUES 
        (
            @{nameof(GuildSettings.GuildId)},
            @{nameof(GuildSettings.GuildName)},
            @{nameof(GuildSettings.DefaultReportChannel)},
            @{nameof(GuildSettings.TrackedMetrics)},
            @{nameof(GuildSettings.TrackedRecords)},
            @{nameof(GuildSettings.TrackedCollectibles)},
            @{nameof(GuildSettings.TrackedProgressions)},
            @{nameof(GuildSettings.SystemReportsEnabled)},
            @{nameof(GuildSettings.SystemReportsOverrideChannel)},
            @{nameof(GuildSettings.Clans)}
        )
        ON CONFLICT (GuildId) DO UPDATE SET
            {nameof(GuildSettings.GuildId)} = @{nameof(GuildSettings.GuildId)},
            {nameof(GuildSettings.GuildName)} = @{nameof(GuildSettings.GuildName)},
            {nameof(GuildSettings.DefaultReportChannel)} = @{nameof(GuildSettings.DefaultReportChannel)},
            {nameof(GuildSettings.TrackedMetrics)} = @{nameof(GuildSettings.TrackedMetrics)},
            {nameof(GuildSettings.TrackedRecords)} = @{nameof(GuildSettings.TrackedRecords)},
            {nameof(GuildSettings.TrackedCollectibles)} = @{nameof(GuildSettings.TrackedCollectibles)},
            {nameof(GuildSettings.TrackedProgressions)} = @{nameof(GuildSettings.TrackedProgressions)},
            {nameof(GuildSettings.SystemReportsEnabled)} = @{nameof(GuildSettings.SystemReportsEnabled)},
            {nameof(GuildSettings.SystemReportsOverrideChannel)} = @{nameof(GuildSettings.SystemReportsOverrideChannel)};
            {nameof(GuildSettings.Clans)} = @{nameof(GuildSettings.Clans)}
        """;
    public static async Task UpsertGuildSettingsAsync(this IDbAccess dbAccess, GuildSettings guildSettings)
    {
        await dbAccess.ExecuteAsync(UpsertGuildSettingsQuery, guildSettings);
    }

    private const string DeleteGuildSettingsQuery =
        $"""
        DELETE FROM Guilds
        WHERE {nameof(GuildSettings.GuildId)} = @{nameof(GuildSettings.GuildId)};
        """;

    public static async Task DeleteGuildSettingsAsync(this IDbAccess dbAccess, ulong guildId)
    {
        await dbAccess.ExecuteAsync(DeleteGuildSettingsQuery, new { GuildId = guildId });
    }
}
