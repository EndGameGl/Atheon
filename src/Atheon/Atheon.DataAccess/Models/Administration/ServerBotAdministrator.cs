using Atheon.DataAccess.Attributes;

namespace Atheon.DataAccess.Models.Administration;

[DapperAutomap]
[AutoTable("ServerBotAdministrators")]
public class ServerBotAdministrator
{
    [AutoColumn(nameof(DiscordGuildId), isPrimaryKey: true, notNull: true)]
    public ulong DiscordGuildId { get; set; }

    [AutoColumn(nameof(DiscordUserId), isPrimaryKey: true, notNull: true)]
    public ulong DiscordUserId { get; set; }
}
