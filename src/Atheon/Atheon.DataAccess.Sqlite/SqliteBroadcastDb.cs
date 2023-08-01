using Atheon.DataAccess.Models.Destiny.Broadcasts;

namespace Atheon.DataAccess.Sqlite;

public class SqliteBroadcastDb : IBroadcastDb
{
    private readonly IDbAccess _dbAccess;

    public SqliteBroadcastDb(IDbAccess dbAccess)
    {
        _dbAccess = dbAccess;
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
                {nameof(DestinyUserProfileCustomBroadcastDbModel.NewValue)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.NewValue)} AND
                {nameof(DestinyUserProfileCustomBroadcastDbModel.MembershipId)} = @{nameof(DestinyUserProfileCustomBroadcastDbModel.MembershipId)};
        """;
    public async Task MarkUserCustomBroadcastSentAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast)
    {
        await _dbAccess.ExecuteAsync(MarkUserCustomBroadcastSentQuery, profileCustomBroadcast);
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
}
