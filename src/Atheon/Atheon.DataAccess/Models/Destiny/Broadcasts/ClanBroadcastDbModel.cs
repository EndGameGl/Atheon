using Atheon.DataAccess.Attributes;
using Atheon.DataAccess.Models.Destiny.Interfaces;

namespace Atheon.DataAccess.Models.Destiny.Broadcasts;

[DapperAutomap]
[AutoTable("DestinyClanBroadcasts")]
public class ClanBroadcastDbModel : IBroadcast
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
    public ClanBroadcastType Type { get; set; }

    [AutoColumn(nameof(OldValue))]
    public string OldValue { get; set; }

    [AutoColumn(nameof(NewValue))]
    public string NewValue { get; set; }

    public static ClanBroadcastDbModel ClanNameChanged(ulong guildId, long clanId, string oldName, string newName)
    {
        return new ClanBroadcastDbModel()
        {
            GuildId = guildId,
            ClanId = clanId,
            Date = DateTime.UtcNow,
            Type = ClanBroadcastType.ClanName,
            WasAnnounced = false,
            OldValue = oldName,
            NewValue = newName
        };
    }

    public static ClanBroadcastDbModel ClanLevelChanged(ulong guildId, long clanId, int oldLevel, int newLevel)
    {
        return new ClanBroadcastDbModel()
        {
            GuildId = guildId,
            ClanId = clanId,
            Date = DateTime.UtcNow,
            Type = ClanBroadcastType.ClanLevel,
            WasAnnounced = false,
            OldValue = oldLevel.ToString(),
            NewValue = newLevel.ToString()
        };
    }

    public static ClanBroadcastDbModel ClanCallSignChanged(ulong guildId, long clanId, string oldCallSign, string newCallSign)
    {
        return new ClanBroadcastDbModel()
        {
            GuildId = guildId,
            ClanId = clanId,
            Date = DateTime.UtcNow,
            Type = ClanBroadcastType.ClanCallsign,
            WasAnnounced = false,
            OldValue = oldCallSign,
            NewValue = newCallSign
        };
    }
}
