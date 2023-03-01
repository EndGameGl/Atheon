using Atheon.Attributes;
using Atheon.Options;

namespace Atheon.Models.Database.Administration;

[DapperAutomap]
[AutoTable("ServerBotAdministrators")]
public class ServerBotAdministrator
{
    [AutoColumn(nameof(DiscordGuildId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong DiscordGuildId { get; set; }

    [AutoColumn(nameof(DiscordUserId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong DiscordUserId { get; set; }
}
