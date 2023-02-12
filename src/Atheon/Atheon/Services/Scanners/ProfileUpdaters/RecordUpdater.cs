using Atheon.Extensions;
using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Quests;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Models.Extensions;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
    public class RecordUpdater : IProfileUpdater
    {
        public bool ReliesOnSecondaryComponents => true;

        public void Update(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {

        }

        public void UpdateSilent(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse)
        {
            foreach (var (recordHash, recordComponent) in profileResponse.ProfileRecords.Data.Records)
            {
                if (dbProfile.Records.TryGetValue(recordHash, out var dbRecord))
                {
                    UpdateRecordDataSilent(dbRecord, recordComponent);
                }
                else
                {
                    dbProfile.Records.Add(recordHash, new DestinyRecordDbModel(recordComponent));
                }
            }

            if (profileResponse.CharacterRecords.Data.Count == 0)
                return;

            var firstCharacterRecords = profileResponse.CharacterRecords.Data.First();

            foreach (var (recordHash, _) in firstCharacterRecords.Value.Records)
            {
                var optimalRecord = profileResponse.CharacterRecords.Data.GetOptimalRecordAcrossCharacters(recordHash);

                if (dbProfile.Records.TryGetValue(recordHash, out var dbRecord))
                {
                    UpdateRecordDataSilent(dbRecord, optimalRecord);
                }
                else
                {
                    dbProfile.Records.Add(recordHash, new DestinyRecordDbModel(optimalRecord));
                }
            }
        }


        private void UpdateRecordDataSilent(
            DestinyRecordDbModel dbRecord,
            DestinyRecordComponent destinyRecordComponent)
        {
            dbRecord.State = destinyRecordComponent.State;

            if (destinyRecordComponent.Objectives.Count > 0 && dbRecord.Objectives is null)
            {
                dbRecord.Objectives = destinyRecordComponent.Objectives.Select(x => new DestinyObjectiveProgressDbModel(x)).ToList();
            }
            else
            {
                for (int i = 0; i < destinyRecordComponent.Objectives.Count; i++)
                {
                    var objective = destinyRecordComponent.Objectives[i];
                    var currentObjective = dbRecord.Objectives!.FirstOrDefault(x => x.ObjectiveHash == objective.Objective.Hash!.Value);

                    if (currentObjective is null)
                    {
                        dbRecord.Objectives!.Add(new DestinyObjectiveProgressDbModel(objective));
                    }
                    else
                    {
                        UpdateObjective(currentObjective, objective);
                    }
                }
            }

            if (destinyRecordComponent.IntervalObjectives.Count > 0 && dbRecord.IntervalObjectives is null)
            {
                dbRecord.IntervalObjectives = destinyRecordComponent.IntervalObjectives.Select(x => new DestinyObjectiveProgressDbModel(x)).ToList();
            }
            else
            {
                for (int i = 0; i < destinyRecordComponent.IntervalObjectives.Count; i++)
                {
                    var objective = destinyRecordComponent.IntervalObjectives[i];
                    var currentObjective = dbRecord.IntervalObjectives!.FirstOrDefault(x => x.ObjectiveHash == objective.Objective.Hash!.Value);

                    if (currentObjective is null)
                    {
                        dbRecord.IntervalObjectives!.Add(new DestinyObjectiveProgressDbModel(objective));
                    }
                    else
                    {
                        UpdateObjective(currentObjective, objective);
                    }
                }
            }

            dbRecord.CompletedCount = destinyRecordComponent.CompletedCount;

        }

        private void UpdateObjective(
            DestinyObjectiveProgressDbModel dbObjective,
            DestinyObjectiveProgress objectiveProgress)
        {
            dbObjective.Progress = objectiveProgress.Progress;
            dbObjective.CompletionValue = objectiveProgress.CompletionValue;
            dbObjective.IsComplete = objectiveProgress.IsComplete;
        }
    }
}
