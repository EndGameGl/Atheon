using Atheon.Models.Database.Destiny;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;
using Atheon.Extensions;
using Atheon.Models.Database.Destiny.Profiles;

namespace Atheon.Services.Scanners.ProfileUpdaters;

public class ProgressionUpdater : IProfileUpdater
{
    public bool ReliesOnSecondaryComponents => false;

    public void Update(
        IBungieClient bungieClient,
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse,
        List<DiscordGuildSettingsDbModel> guildSettings)
    {
        var character = profileResponse.CharacterProgressions.Data.First().Value;

        foreach (var (progressionPointer, _) in character.Progressions)
        {
            var progressionHash = progressionPointer.Hash.GetValueOrDefault();
            var progressionData = profileResponse.CharacterProgressions.Data.GetMostCompletedProgressionAcrossCharacters(progressionHash, bungieClient);

            if (dbProfile.Progressions.TryGetValue(progressionHash, out var savedProgressionData))
            {
                savedProgressionData.CurrentProgress = progressionData.CurrentProgress;
                savedProgressionData.CurrentResetCount = progressionData.CurrentResetCount;
            }
            else
            {
                dbProfile.Progressions.Add(progressionHash, new DestinyProgressionDbModel(progressionData));
            }
        }
    }

    public void UpdateSilent(
        IBungieClient bungieClient,
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse)
    {
        var character = profileResponse.CharacterProgressions.Data.First().Value;

        foreach (var (progressionPointer, _) in character.Progressions)
        {
            var progressionHash = progressionPointer.Hash.GetValueOrDefault();
            var progressionData = profileResponse.CharacterProgressions.Data.GetMostCompletedProgressionAcrossCharacters(progressionHash, bungieClient);

            if (dbProfile.Progressions.TryGetValue(progressionHash, out var savedProgressionData))
            {
                savedProgressionData.CurrentProgress = progressionData.CurrentProgress;
                savedProgressionData.CurrentResetCount = progressionData.CurrentResetCount;
            }
            else
            {
                dbProfile.Progressions.Add(progressionHash, new DestinyProgressionDbModel(progressionData));
            }
        }
    }
}
