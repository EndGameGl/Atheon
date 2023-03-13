using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
    public class ComputedDataUpdater : IProfileUpdater
    {
        private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

        public bool ReliesOnSecondaryComponents => true;

        public int Priority => 1;

        public ComputedDataUpdater(DestinyDefinitionDataService destinyDefinitionDataService)
        {
            _destinyDefinitionDataService = destinyDefinitionDataService;
        }

        public async Task Update(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {
            dbProfile.ComputedData ??= new DestinyComputedData();
            await UpdateComputedData(dbProfile, profileResponse);
        }

        public async Task UpdateSilent(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse)
        {
            dbProfile.ComputedData ??= new DestinyComputedData();
            await UpdateComputedData(dbProfile, profileResponse);
        }

        private async Task UpdateComputedData(
            DestinyProfileDbModel dbModel,
            DestinyProfileResponse profileResponse)
        {
            dbModel.ComputedData!.Drystreaks = new Dictionary<uint, int>();
            foreach (var (collectibleHash, metricHash) in Destiny2Metadata.DryStreakItemSettings)
            {
                if (!profileResponse.Metrics.Data.Metrics.TryGetValue(metricHash, out var metricComponent))
                    continue;

                var progress = metricComponent.ObjectiveProgress.Progress ?? 0;

                if (!dbModel.Collectibles.Contains(collectibleHash))
                {
                    dbModel.ComputedData.Drystreaks[collectibleHash] = progress;
                }
                else
                {
                    dbModel.ComputedData.Drystreaks.Remove(collectibleHash);
                }
            }

            dbModel.ComputedData.Titles = new Dictionary<uint, int>();
            var titleHashes = await _destinyDefinitionDataService.GetTitleHashesCachedAsync();
            var profileRecords = profileResponse.ProfileRecords.Data.Records;
            foreach (var (titleHash, gildHash) in titleHashes)
            {
                int completions = 0;
                if (profileRecords.TryGetValue(titleHash, out var recordComponent)
                    && !recordComponent.State.HasFlag(DestinyRecordState.ObjectiveNotCompleted))
                {
                    completions++;

                    if (gildHash.HasValue &&
                        profileRecords.TryGetValue(gildHash.Value, out var gildRecordComponent))
                    {
                        completions += (gildRecordComponent.CompletedCount ?? 0);
                    }
                }

                dbModel.ComputedData.Titles[titleHash] = completions;
            }

            if (dbModel.Records.TryGetValue(DefinitionHashes.Records.PathtoPower, out var powerRecord))
            {
                dbModel.ComputedData.PowerLevel = powerRecord.IntervalObjectives?.FirstOrDefault()?.Progress;
            }

            if (dbModel.Records.TryGetValue(DefinitionHashes.Records.ArtifactPowerBonus, out var artifactPowerRecord))
            {
                dbModel.ComputedData.ArtifactPowerLevel = artifactPowerRecord.Objectives?.FirstOrDefault()?.Progress;
            }

            dbModel.ComputedData.LifetimeScore = profileResponse.ProfileRecords.Data.LifetimeScore;
            dbModel.ComputedData.ActiveScore = profileResponse.ProfileRecords.Data.ActiveScore;
            dbModel.ComputedData.LegacyScore = profileResponse.ProfileRecords.Data.LegacyScore;
            dbModel.ComputedData.TotalTitlesEarned = dbModel.ComputedData.Titles.Count(x => x.Value > 0);
        }
    }
}
