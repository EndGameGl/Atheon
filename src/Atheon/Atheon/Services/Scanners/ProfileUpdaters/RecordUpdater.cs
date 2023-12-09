using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Destiny2.Metadata;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using DotNetBungieAPI.Models.Destiny.Quests;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
    public class RecordUpdater : IProfileUpdater
    {
        private readonly ICommonEvents _commonEvents;
        private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

        public RecordUpdater(
            ICommonEvents commonEvents,
            DestinyDefinitionDataService destinyDefinitionDataService)
        {
            _commonEvents = commonEvents;
            _destinyDefinitionDataService = destinyDefinitionDataService;
        }

        public bool ReliesOnSecondaryComponents => true;
        public int Priority => 0;

        public async Task Update(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {
            var titleAndGildHashes = await _destinyDefinitionDataService.GetTitleHashesCachedAsync();
            foreach (var (recordHash, recordComponent) in profileResponse.ProfileRecords.Data.Records)
            {
                if (dbProfile.Records.TryGetValue(recordHash, out var dbRecord))
                {
                    if (dbRecord.State.HasFlag(DestinyRecordState.ObjectiveNotCompleted) &&
                        !recordComponent.State.HasFlag(DestinyRecordState.ObjectiveNotCompleted))
                    {
                        if (titleAndGildHashes.Any(x => x.TitleRecordHash == recordHash))
                        {
                            foreach (var guildSetting in guildSettings)
                            {
                                if (!guildSetting.TrackedRecords.IsReported)
                                    continue;

                                _commonEvents.ProfileBroadcasts.Publish(new DestinyUserProfileBroadcastDbModel()
                                {
                                    Type = ProfileBroadcastType.Title,
                                    ClanId = dbProfile.ClanId.GetValueOrDefault(),
                                    Date = DateTime.UtcNow,
                                    GuildId = guildSetting.GuildId,
                                    DefinitionHash = recordHash,
                                    MembershipId = dbProfile.MembershipId,
                                    WasAnnounced = false
                                });
                            }
                        }
                        else if (titleAndGildHashes.Any(x => x.TitleGildRecordHash == recordHash))
                        {
                            var parentTitleRecord = bungieClient
                                .Repository
                                .Search<DestinyRecordDefinition>(def => def.TitleInfo?.GildingTrackingRecord.Hash == recordHash)
                                .FirstOrDefault();

                            if (parentTitleRecord is null)
                                continue;

                            foreach (var guildSetting in guildSettings)
                            {
                                if (!guildSetting.TrackedRecords.IsReported)
                                    continue;

                                _commonEvents.ProfileBroadcasts.Publish(new DestinyUserProfileBroadcastDbModel()
                                {
                                    Type = ProfileBroadcastType.GildedTitle,
                                    ClanId = dbProfile.ClanId.GetValueOrDefault(),
                                    Date = DateTime.UtcNow,
                                    GuildId = guildSetting.GuildId,
                                    DefinitionHash = recordHash,
                                    MembershipId = dbProfile.MembershipId,
                                    WasAnnounced = false,
                                    AdditionalData = new Dictionary<string, string>
                                    {
                                        ["parentTitleHash"] = parentTitleRecord.Hash.ToString(),
                                        ["gildedCount"] = recordComponent.CompletedCount.GetValueOrDefault().ToString()
                                    }
                                });
                            }
                        }
                        else
                        {
                            foreach (var guildSetting in guildSettings)
                            {
                                if (!guildSetting.TrackedRecords.IsReported)
                                    continue;

                                if (!guildSetting.TrackedRecords.TrackedHashes.Contains(recordHash))
                                    continue;

                                _commonEvents.ProfileBroadcasts.Publish(new DestinyUserProfileBroadcastDbModel()
                                {
                                    Type = ProfileBroadcastType.Triumph,
                                    ClanId = dbProfile.ClanId.GetValueOrDefault(),
                                    Date = DateTime.UtcNow,
                                    GuildId = guildSetting.GuildId,
                                    DefinitionHash = recordHash,
                                    MembershipId = dbProfile.MembershipId,
                                    WasAnnounced = false
                                });
                            }
                        }
                    }
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

        public async Task UpdateSilent(
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
