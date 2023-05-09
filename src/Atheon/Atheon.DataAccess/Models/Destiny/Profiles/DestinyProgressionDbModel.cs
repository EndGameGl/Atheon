using DotNetBungieAPI.Models.Destiny.Progressions;
using System.Text.Json.Serialization;

namespace Atheon.DataAccess.Models.Destiny.Profiles;

public class DestinyProgressionDbModel
{
    [JsonPropertyName("currentProgress")]
    public int CurrentProgress { get; set; }

    [JsonPropertyName("currentResetCount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CurrentResetCount { get; set; }

    [JsonPropertyName("level"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Level { get; set; }

    public DestinyProgressionDbModel() { }

    public DestinyProgressionDbModel(DestinyProgression destinyProgression)
    {
        CurrentProgress = destinyProgression.CurrentProgress;
        CurrentResetCount = destinyProgression.CurrentResetCount;
        Level = destinyProgression.Level;
    }
}
