using DotNetBungieAPI.Models.Destiny;
using System.Text.Json.Serialization;

namespace Atheon.Models.Database.Destiny.Profiles;

public class DestinyRecordDbModel
{
    [JsonPropertyName("state")]
    public DestinyRecordState State { get; set; }

    [JsonPropertyName("objectives"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DestinyObjectiveProgressDbModel>? Objectives { get; set; }

    [JsonPropertyName("intervalObjectives"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DestinyObjectiveProgressDbModel>? IntervalObjectives { get; set; }

    [JsonPropertyName("completedCount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CompletedCount { get; set; }
}
