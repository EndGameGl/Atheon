using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Destiny2.Metadata;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters;

public class ProgressionUpdater : IProfileUpdater
{
    public bool ReliesOnSecondaryComponents => false;
    public int Priority => 0;

    public async Task Update(
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
                savedProgressionData.Level = progressionData.Level;
            }
            else
            {
                dbProfile.Progressions.Add(progressionHash, new DestinyProgressionDbModel(progressionData));
            }
        }
    }

    public async Task UpdateSilent(
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
                savedProgressionData.Level = progressionData.Level;
            }
            else
            {
                dbProfile.Progressions.Add(progressionHash, new DestinyProgressionDbModel(progressionData));
            }
        }
    }
}
