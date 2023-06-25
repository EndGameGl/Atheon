using Atheon.DataAccess.Attributes;

namespace Atheon.DataAccess.Models.GroupSearch;

[AutoTable("DestinyGroupSearch")]
[DapperAutomap]
public class DestinyGroupSearch
{
    [AutoColumn(nameof(DiscordChannelId), isPrimaryKey: true, notNull: true)]
    public ulong DiscordChannelId { get; set; }

    [AutoColumn(nameof(DiscordMessageId), isPrimaryKey: true, notNull: true)]
    public ulong DiscordMessageId { get; set; }

    [AutoColumn(nameof(ActivityHash), isPrimaryKey: true, notNull: true)]
    public ulong ActivityHash { get; set; }

    [AutoColumn(nameof(DiscordMembers), notNull: true)]
    public HashSet<ulong> DiscordMembers { get; set; }

    [AutoColumn(nameof(CreatedTime), notNull: true)]
    public DateTime CreatedTime { get; set; }

    [AutoColumn(nameof(DueTo), notNull: true)]
    public DateTime DueTo { get; set; }

    [AutoColumn(nameof(IsOpen), notNull: true)]
    public bool IsOpen { get; set; }
}
