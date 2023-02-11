using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.Destiny.Progressions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Extensions;

public static class DestinyExtensions
{
    public static bool HasPublicRecords(this DestinyProfileResponse profileResponse)
    {
        return profileResponse.ProfileRecords.Data is not null;
    }

    public static DestinyProgression GetMostCompletedProgressionAcrossCharacters(
        this IDictionary<long, DestinyCharacterProgressionComponent> characterProgressions,
        uint progressionHash,
        IBungieClient bungieClient)
    {
        var progressionComponents = new List<DestinyProgression>(characterProgressions.Count);

        foreach (var (_, progressions) in characterProgressions)
        {
            if (progressions.Progressions.TryGetValue(progressionHash, out var destinyProgression))
            {
                progressionComponents.Add(destinyProgression);
            }
        }

        if (bungieClient.Repository.TryGetDestinyDefinition<DestinyProgressionDefinition>(
            progressionHash,
            BungieLocales.EN,
            out var progressionDefinition))
        {
            if (progressionComponents.Any(x => x.CurrentResetCount is not null))
            {
                var totalProgressPoints = progressionDefinition.Steps.Sum(x => x.ProgressTotal);
                return progressionComponents.MaxBy(x => x.CurrentResetCount.GetValueOrDefault() * totalProgressPoints + x.CurrentProgress)!;
            }
            else
            {
                return progressionComponents.MaxBy(x => x.CurrentProgress)!;
            }
        }
        else
        {
            return progressionComponents.MaxBy(x => x.CurrentProgress)!;
        }

    }
}
