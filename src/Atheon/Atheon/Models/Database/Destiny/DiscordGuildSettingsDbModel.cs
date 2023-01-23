using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.GroupsV2;
using Atheon.Attributes;
using Atheon.Options;

namespace Atheon.Models.Database.Destiny;

/// <summary>
///     Discord server related data
/// </summary>
[AutoTable("Guilds")]
[DapperAutomap]
public class DiscordGuildSettingsDbModel
{
    /// <summary>
    ///     Discord server guild ID
    /// </summary>
    [AutoColumn(nameof(GuildId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Discord server guild name
    /// </summary>
    [AutoColumn(nameof(GuildName), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string GuildName { get; set; }

    /// <summary>
    ///     Id of Discord channel all reports are sent to
    /// </summary>
    [AutoColumn(nameof(DefaultReportChannel), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong? DefaultReportChannel { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyMetricDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedMetrics), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public DefinitionTrackSettings<DestinyMetricDefinition> TrackedMetrics { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyRecordDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedRecords), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public DefinitionTrackSettings<DestinyRecordDefinition> TrackedRecords { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyCollectibleDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedCollectibles), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public DefinitionTrackSettings<DestinyCollectibleDefinition> TrackedCollectibles { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyProgressionDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedProgressions), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public DefinitionTrackSettings<DestinyProgressionDefinition> TrackedProgressions { get; set; }

    /// <summary>
    ///     Whether system reports are sent to Discord channel
    /// </summary>
    [AutoColumn(nameof(SystemReportsEnabled), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.BOOLEAN)]
    public bool SystemReportsEnabled { get; set; }

    /// <summary>
    ///     Id of Discord channel all system reports are sent to
    /// </summary>
    [AutoColumn(nameof(SystemReportsOverrideChannel), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.UNSIGNED_BIG_INT)]
    public ulong? SystemReportsOverrideChannel { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="GroupV2"/> IDs that are linked to this Discord guild
    /// </summary>
    [AutoColumn(nameof(Clans), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public HashSet<long> Clans { get; set; }

    public static DiscordGuildSettingsDbModel CreateDefault(ulong guildId, string guildName)
    {
        return new DiscordGuildSettingsDbModel()
        {
            GuildId = guildId,
            GuildName = guildName,
            DefaultReportChannel = null,
            SystemReportsEnabled = false,
            SystemReportsOverrideChannel = null,
            Clans = new HashSet<long>(),
            TrackedCollectibles = DefinitionTrackSettings<DestinyCollectibleDefinition>.CreateDefault(),
            TrackedMetrics = DefinitionTrackSettings<DestinyMetricDefinition>.CreateDefault(),
            TrackedProgressions = DefinitionTrackSettings<DestinyProgressionDefinition>.CreateDefault(),
            TrackedRecords = DefinitionTrackSettings<DestinyRecordDefinition>.CreateDefault()
        };
    }
}
