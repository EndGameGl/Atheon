using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.GroupsV2;
using System.Text.Json.Serialization;
using Atheon.DataAccess.Attributes;
using Atheon.DataAccess.Models.Discord;

namespace Atheon.DataAccess.Models.Destiny;

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
    [AutoColumn(nameof(GuildId), isPrimaryKey: true, notNull: true)]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Discord server guild name
    /// </summary>
    [AutoColumn(nameof(GuildName))]
    public string GuildName { get; set; }

    /// <summary>
    ///     Id of Discord channel all reports are sent to
    /// </summary>
    [AutoColumn(nameof(DefaultReportChannel))]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public ulong? DefaultReportChannel { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyMetricDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedMetrics))]
    public DefinitionTrackSettings<DestinyMetricDefinition> TrackedMetrics { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyRecordDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedRecords))]
    public DefinitionTrackSettings<DestinyRecordDefinition> TrackedRecords { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyCollectibleDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedCollectibles))]
    public DefinitionTrackSettings<DestinyCollectibleDefinition> TrackedCollectibles { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyProgressionDefinition"/> hashes
    /// </summary>
    [AutoColumn(nameof(TrackedProgressions))]
    public DefinitionTrackSettings<DestinyProgressionDefinition> TrackedProgressions { get; set; }

    /// <summary>
    ///     Whether system reports are sent to Discord channel
    /// </summary>
    [AutoColumn(nameof(SystemReportsEnabled))]
    public bool SystemReportsEnabled { get; set; }

    /// <summary>
    ///     Id of Discord channel all system reports are sent to
    /// </summary>
    [AutoColumn(nameof(SystemReportsOverrideChannel))]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public ulong? SystemReportsOverrideChannel { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="GroupV2"/> IDs that are linked to this Discord guild
    /// </summary>
    [AutoColumn(nameof(Clans))]
    public HashSet<long> Clans { get; set; }

    [AutoColumn(nameof(ReportClanChanges), notNull: true)]
    public bool ReportClanChanges { get; set; }

    [AutoColumn(nameof(DestinyManifestLocale))]
    public DiscordDestinyLanguageEnum DestinyManifestLocale { get; set; } = DiscordDestinyLanguageEnum.English;

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
            TrackedRecords = DefinitionTrackSettings<DestinyRecordDefinition>.CreateDefault(),
            ReportClanChanges = true
        };
    }
}
