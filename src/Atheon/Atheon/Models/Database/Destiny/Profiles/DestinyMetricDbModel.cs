using DotNetBungieAPI.Models.Destiny.Components;
using System.Text.Json.Serialization;

namespace Atheon.Models.Database.Destiny.Profiles;

public class DestinyMetricDbModel
{
    [JsonPropertyName("progress")]
    public DestinyObjectiveProgressDbModel Progress { get; set; }

    public static DestinyMetricDbModel FromMetricComponent(DestinyMetricComponent destinyMetricComponent)
    {
        return new DestinyMetricDbModel()
        {
            Progress = new DestinyObjectiveProgressDbModel(destinyMetricComponent.ObjectiveProgress)
        };
    }
}
