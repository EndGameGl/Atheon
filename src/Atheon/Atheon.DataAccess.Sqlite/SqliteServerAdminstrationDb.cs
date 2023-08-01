using Atheon.DataAccess.Models.Administration;

namespace Atheon.DataAccess.Sqlite;

public class SqliteServerAdminstrationDb : IServerAdminstrationDb
{
    private readonly IDbAccess _dbAccess;

    public SqliteServerAdminstrationDb(IDbAccess dbAccess)
    {
        _dbAccess = dbAccess;
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
}
