using Atheon.DataAccess.Models.GroupSearch;

namespace Atheon.DataAccess.Sqlite;

public class SqliteDestinyGroupSearchDb : IDestinyGroupSearchDb
{
    private readonly IDbAccess _dbAccess;

    public SqliteDestinyGroupSearchDb(IDbAccess dbAccess)
    {
        _dbAccess = dbAccess;
    }

    public Task CloseGroupAsync(DestinyGroupSearch group)
    {
        throw new NotImplementedException();
    }

    private const string GetGroupQuery =
        $"""
        SELECT * FROM DestinyGroupSearch
        WHERE 
            {nameof(DestinyGroupSearch.DiscordChannelId)} = @{nameof(DestinyGroupSearch.DiscordChannelId)} AND
            {nameof(DestinyGroupSearch.DiscordMessageId)} = @{nameof(DestinyGroupSearch.DiscordMessageId)}
        """;
    public async Task<DestinyGroupSearch?> GetGroupAsync(ulong channelId, ulong messageId)
    {
        return await _dbAccess.QueryFirstOrDefaultAsync<DestinyGroupSearch?>(GetGroupQuery, new
        {
            DiscordChannelId = channelId,
            DiscordMessageId = messageId
        });
    }

    private const string InsertGroupQuery =
        $"""
        INSERT INTO DestinyGroupSearch
        (
            {nameof(DestinyGroupSearch.DiscordChannelId)},
            {nameof(DestinyGroupSearch.DiscordMessageId)},
            {nameof(DestinyGroupSearch.CreatedTime)},
            {nameof(DestinyGroupSearch.DueTo)},
            {nameof(DestinyGroupSearch.ActivityHash)},
            {nameof(DestinyGroupSearch.DiscordMembers)},
            {nameof(DestinyGroupSearch.IsOpen)}
        )
        VALUES 
        (
            @{nameof(DestinyGroupSearch.DiscordChannelId)},
            @{nameof(DestinyGroupSearch.DiscordMessageId)},
            @{nameof(DestinyGroupSearch.CreatedTime)},
            @{nameof(DestinyGroupSearch.DueTo)},
            @{nameof(DestinyGroupSearch.ActivityHash)},
            @{nameof(DestinyGroupSearch.DiscordMembers)},
            @{nameof(DestinyGroupSearch.IsOpen)}
        );
        """;
    public async Task InsertGroupAsync(DestinyGroupSearch group)
    {
        await _dbAccess.ExecuteAsync(InsertGroupQuery, group);
    }


    private const string UpdateGroupMembersQuery =
        $"""
        UPDATE DestinyGroupSearch
        SET
            {nameof(DestinyGroupSearch.DiscordMembers)} = @{nameof(DestinyGroupSearch.DiscordMembers)}
        WHERE 
            {nameof(DestinyGroupSearch.DiscordChannelId)} = @{nameof(DestinyGroupSearch.DiscordChannelId)} AND
            {nameof(DestinyGroupSearch.DiscordMessageId)} = @{nameof(DestinyGroupSearch.DiscordMessageId)}
        """;
    public async Task UpdateGroupMembersAsync(DestinyGroupSearch group)
    {
        await _dbAccess.ExecuteAsync(UpdateGroupMembersQuery, new
        {
            DiscordChannelId = group.DiscordChannelId,
            DiscordMessageId = group.DiscordMessageId,
            DiscordMembers = group.DiscordMembers
        });
    }
}
