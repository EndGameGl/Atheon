using Atheon.DataAccess.Attributes;
using DotNetBungieAPI.Models;

namespace Atheon.DataAccess.Models.Destiny.Links;

[DapperAutomap]
[AutoTable("DiscordToDestinyProfileLinks")]
public class DiscordToDestinyProfileLink
{
    [AutoColumn(nameof(DiscordUserId), isPrimaryKey: true, notNull: true)]
    public ulong DiscordUserId { get; set; }

    [AutoColumn(nameof(DestinyMembershipId), notNull: true)]
    public long DestinyMembershipId { get; set; }

    [AutoColumn(nameof(BungieMembershipType), notNull: true)]
    public BungieMembershipType BungieMembershipType { get; set; }
}
