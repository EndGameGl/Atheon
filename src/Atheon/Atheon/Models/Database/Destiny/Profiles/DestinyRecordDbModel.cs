using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Components;
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

    public DestinyRecordDbModel() { }

    public DestinyRecordDbModel(DestinyRecordComponent destinyRecordComponent) 
    {
        State = destinyRecordComponent.State;
        Objectives = destinyRecordComponent.Objectives.Count > 0 ? 
                destinyRecordComponent.Objectives.Select(x => new DestinyObjectiveProgressDbModel(x)).ToList() : 
                null;
        IntervalObjectives = destinyRecordComponent.IntervalObjectives.Count > 0 ?
                destinyRecordComponent.IntervalObjectives.Select(x => new DestinyObjectiveProgressDbModel(x)).ToList() :
                null;
        CompletedCount = destinyRecordComponent.CompletedCount;
    }
}
