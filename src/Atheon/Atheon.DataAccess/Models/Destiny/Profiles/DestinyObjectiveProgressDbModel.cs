﻿using DotNetBungieAPI.Models.Destiny.Quests;
using System.Text.Json.Serialization;

namespace Atheon.DataAccess.Models.Destiny.Profiles;

public class DestinyObjectiveProgressDbModel
{
    [JsonPropertyName("objectiveHash")]
    public uint ObjectiveHash { get; set; }

    [JsonPropertyName("progress"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Progress { get; set; }

    [JsonPropertyName("completionValue")]
    public int CompletionValue { get; set; }

    [JsonPropertyName("complete")]
    public bool IsComplete { get; set; }

    public DestinyObjectiveProgressDbModel() { }

    public DestinyObjectiveProgressDbModel(
        DestinyObjectiveProgress destinyObjectiveProgress)
    {
        ObjectiveHash = destinyObjectiveProgress.Objective.Hash.GetValueOrDefault();
        Progress = destinyObjectiveProgress.Progress;
        CompletionValue = destinyObjectiveProgress.CompletionValue;
        IsComplete = destinyObjectiveProgress.IsComplete;
    }
}
