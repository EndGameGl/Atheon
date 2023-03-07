using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
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
        }
    }
}
