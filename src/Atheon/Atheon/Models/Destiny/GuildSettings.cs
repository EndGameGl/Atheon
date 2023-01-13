using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.GroupsV2;
using Discord.WebSocket;

namespace Atheon.Models.Destiny;

/// <summary>
///     Discord server related data
/// </summary>
public class GuildSettings
{
    /// <summary>
    ///     Discord server guild ID
    /// </summary>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Discord server guild name
    /// </summary>
    public string GuildName { get; set; }

    /// <summary>
    ///     Id of Discord channel all reports are sent to
    /// </summary>
    public ulong? DefaultReportChannel { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyMetricDefinition"/> hashes
    /// </summary>
    public DefinitionTrackSettings<DestinyMetricDefinition> TrackedMetrics { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyRecordDefinition"/> hashes
    /// </summary>
    public DefinitionTrackSettings<DestinyRecordDefinition> TrackedRecords { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyCollectibleDefinition"/> hashes
    /// </summary>
    public DefinitionTrackSettings<DestinyCollectibleDefinition> TrackedCollectibles { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="DestinyProgressionDefinition"/> hashes
    /// </summary>
    public DefinitionTrackSettings<DestinyProgressionDefinition> TrackedProgressions { get; set; }

    /// <summary>
    ///     Whether system reports are sent to Discord channel
    /// </summary>
    public bool SystemReportsEnabled { get; set; }

    /// <summary>
    ///     Id of Discord channel all system reports are sent to
    /// </summary>
    public ulong? SystemReportsOverrideChannel { get; set; }

    /// <summary>
    ///     Destiny 2 <see cref="GroupV2"/> IDs that are linked to this Discord guild
    /// </summary>
    public HashSet<long> Clans { get; set; }

    public static GuildSettings CreateDefault(ulong guildId, string guildName)
    {
        return new GuildSettings()
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
