using Atheon.Attributes;
using Atheon.Models.Database.Destiny.Interfaces;
using Atheon.Options;

namespace Atheon.Models.Database.Destiny.Broadcasts;

[DapperAutomap]
[AutoTable("DestinyUserBroadcasts")]
public class DestinyUserProfileBroadcastDbModel : IBroadcast
{
    [AutoColumn(nameof(GuildId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong GuildId { get; set; }

    [AutoColumn(nameof(ClanId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long ClanId { get; set; }

    [AutoColumn(nameof(WasAnnounced), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.BOOLEAN)]
    public bool WasAnnounced { get; set; }

    [AutoColumn(nameof(Date), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime Date { get; set; }

    [AutoColumn(nameof(Type), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.SMALLINT)]
    public ProfileBroadcastType Type { get; set; }

    [AutoColumn(nameof(MembershipId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long MembershipId { get; set; }

    [AutoColumn(nameof(DefinitionHash), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public uint DefinitionHash { get; set; }

    [AutoColumn(nameof(AdditionalData), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public Dictionary<string, string> AdditionalData { get; set; }
}
