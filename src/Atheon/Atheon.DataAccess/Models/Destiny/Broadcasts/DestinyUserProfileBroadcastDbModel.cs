using Atheon.DataAccess.Attributes;
using Atheon.DataAccess.Models.Destiny.Interfaces;

namespace Atheon.DataAccess.Models.Destiny.Broadcasts;

[DapperAutomap]
[AutoTable("DestinyUserBroadcasts")]
public class DestinyUserProfileBroadcastDbModel : IBroadcast
{
    [AutoColumn(nameof(GuildId), isPrimaryKey: true)]
    public ulong GuildId { get; set; }

    [AutoColumn(nameof(ClanId), isPrimaryKey: true)]
    public long ClanId { get; set; }

    [AutoColumn(nameof(WasAnnounced), notNull: true)]
    public bool WasAnnounced { get; set; }

    [AutoColumn(nameof(Date), notNull: true)]
    public DateTime Date { get; set; }

    [AutoColumn(nameof(Type), isPrimaryKey: true, notNull: true)]
    public ProfileBroadcastType Type { get; set; }

    [AutoColumn(nameof(MembershipId), isPrimaryKey: true, notNull: true)]
    public long MembershipId { get; set; }

    [AutoColumn(nameof(DefinitionHash), isPrimaryKey: true, notNull: true)]
    public uint DefinitionHash { get; set; }

    [AutoColumn(nameof(AdditionalData))]
    public Dictionary<string, string>? AdditionalData { get; set; }
}
