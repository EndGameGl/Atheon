using Atheon.Attributes;
using Atheon.Models.Database.Destiny.Interfaces;
using Atheon.Options;

namespace Atheon.Models.Database.Destiny.Broadcasts;

[DapperAutomap]
[AutoTable("DestinyClanBroadcasts")]
public class ClanBroadcastDbModel : IBroadcast
{
    [AutoColumn(nameof(GuildId), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong GuildId { get; set; }

    [AutoColumn(nameof(ClanId), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long ClanId { get; set; }

    [AutoColumn(nameof(WasAnnounced), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.BOOLEAN)]
    public bool WasAnnounced { get; set; }

    [AutoColumn(nameof(Date), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime Date { get; set; }

    [AutoColumn(nameof(Type), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.DEFAULT_VALUE)]
    public ClanBroadcastType Type { get; set; }

    [AutoColumn(nameof(OldValue), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string OldValue { get; set; }

    [AutoColumn(nameof(NewValue), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string NewValue { get; set; }
}
