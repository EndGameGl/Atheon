using Atheon.DataAccess.Attributes;

namespace Atheon.DataAccess.Models.Destiny.Broadcasts;

[DapperAutomap]
[AutoTable("DestinyUserCustomBroadcasts")]
public class DestinyUserProfileCustomBroadcastDbModel
{
    [AutoColumn(nameof(GuildId), notNull: true)]
    public ulong GuildId { get; set; }

    [AutoColumn(nameof(ClanId), notNull: true)]
    public long ClanId { get; set; }

    [AutoColumn(nameof(WasAnnounced), notNull: true)]
    public bool WasAnnounced { get; set; }

    [AutoColumn(nameof(Date), notNull: true)]
    public DateTime Date { get; set; }

    [AutoColumn(nameof(Type), notNull: true)]
    public ProfileCustomBroadcastType Type { get; set; }

    [AutoColumn(nameof(MembershipId), notNull: true)]
    public long MembershipId { get; set; }

    [AutoColumn(nameof(OldValue))]
    public string OldValue { get; set; }

    [AutoColumn(nameof(NewValue))]
    public string NewValue { get; set; }

    [AutoColumn(nameof(AdditionalData))]
    public Dictionary<string, string>? AdditionalData { get; set; }
}
