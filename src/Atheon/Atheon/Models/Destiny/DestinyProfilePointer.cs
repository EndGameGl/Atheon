using DotNetBungieAPI.Models;

namespace Atheon.Models.Destiny;

public class DestinyProfilePointer
{
    public long MembershipId { get; set; }
    public BungieMembershipType MembershipType { get; set; }
}
