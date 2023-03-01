using Atheon.Attributes;
using Atheon.Options;
using DotNetBungieAPI.Models;

namespace Atheon.Models.Database.Destiny.Links;

[DapperAutomap]
[AutoTable("DiscordToDestinyProfileLinks")]
public class DiscordToDestinyProfileLink
{
    [AutoColumn(nameof(DiscordUserId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong DiscordUserId { get; set; }

    [AutoColumn(nameof(DestinyMembershipId), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long DestinyMembershipId { get; set; }

    [AutoColumn(nameof(BungieMembershipType), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.INT)]
    public BungieMembershipType BungieMembershipType { get; set; }
}
