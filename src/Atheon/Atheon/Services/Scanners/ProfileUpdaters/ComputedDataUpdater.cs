using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Destiny2.Metadata;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
    public class ComputedDataUpdater : IProfileUpdater
    {
        private object test = new object();

        private readonly HashSet<uint> _weaponBucketHashes =
            new()
            {
                DefinitionHashes.InventoryBuckets.KineticWeapons,
                DefinitionHashes.InventoryBuckets.EnergyWeapons,
                DefinitionHashes.InventoryBuckets.PowerWeapons
            };

        private readonly HashSet<uint> _armorBucketHashes =
            new()
            {
                DefinitionHashes.InventoryBuckets.Helmet,
                DefinitionHashes.InventoryBuckets.Gauntlets,
                DefinitionHashes.InventoryBuckets.ChestArmor,
                DefinitionHashes.InventoryBuckets.LegArmor,
                DefinitionHashes.InventoryBuckets.ClassArmor
            };

        private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
        private readonly ILogger<ComputedDataUpdater> _logger;

        public bool ReliesOnSecondaryComponents => true;

        public int Priority => 1;

        public ComputedDataUpdater(
            DestinyDefinitionDataService destinyDefinitionDataService,
            ILogger<ComputedDataUpdater> logger
        )
        {
            _destinyDefinitionDataService = destinyDefinitionDataService;
            _logger = logger;
        }

        public async Task Update(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings
        )
        {
            dbProfile.ComputedData ??= new DestinyComputedData();
            await UpdateComputedData(dbProfile, profileResponse);
        }

        public async Task UpdateSilent(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse
        )
        {
            dbProfile.ComputedData ??= new DestinyComputedData();
            await UpdateComputedData(dbProfile, profileResponse);
        }

        private async Task UpdateComputedData(
            DestinyProfileDbModel dbModel,
            DestinyProfileResponse profileResponse
        )
        {
            dbModel.ComputedData!.Drystreaks = new Dictionary<uint, int>();
            foreach (var (collectibleHash, metricHash) in Destiny2Metadata.DryStreakItemSettings)
            {
                if (
                    !profileResponse.Metrics.Data.Metrics.TryGetValue(
                        metricHash,
                        out var metricComponent
                    )
                )
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
                if (
                    profileRecords.TryGetValue(titleHash, out var recordComponent)
                    && !recordComponent.State.HasFlag(DestinyRecordState.ObjectiveNotCompleted)
                )
                {
                    completions++;

                    if (
                        gildHash.HasValue
                        && profileRecords.TryGetValue(gildHash.Value, out var gildRecordComponent)
                    )
                    {
                        completions += (gildRecordComponent.CompletedCount ?? 0);
                    }
                }

                dbModel.ComputedData.Titles[titleHash] = completions;
            }

            bool failedToGetInvenoryCalculations = true;
            try
            {
                if (
                    profileResponse.ProfileInventory.Data?.Items.Count > 0
                    && profileResponse.ItemComponents?.Instances.Data.Count > 0
                )
                {
                    dbModel.ComputedData.PowerLevel = CalculateHighestLightBasedOnInventory(
                        profileResponse
                    );
                    failedToGetInvenoryCalculations = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate light based on inventory");
            }

            if (
                failedToGetInvenoryCalculations
                && dbModel.Records.TryGetValue(
                    DefinitionHashes.Records.PathtoPower,
                    out var powerRecord
                )
            )
            {
                dbModel.ComputedData.PowerLevel = powerRecord.IntervalObjectives
                    ?.FirstOrDefault()
                    ?.Progress;
            }

            if (
                dbModel.Records.TryGetValue(
                    DefinitionHashes.Records.ArtifactPowerBonus_3093587483,
                    out var artifactPowerRecord
                )
            )
            {
                dbModel.ComputedData.ArtifactPowerLevel = artifactPowerRecord.Objectives
                    ?.FirstOrDefault()
                    ?.Progress;
            }

            dbModel.ComputedData.LifetimeScore = profileResponse.ProfileRecords.Data.LifetimeScore;
            dbModel.ComputedData.ActiveScore = profileResponse.ProfileRecords.Data.ActiveScore;
            dbModel.ComputedData.LegacyScore = profileResponse.ProfileRecords.Data.LegacyScore;
            dbModel.ComputedData.TotalTitlesEarned = dbModel.ComputedData.Titles.Count(
                x => x.Value > 0
            );
        }

        private int CalculateHighestLightBasedOnInventory(DestinyProfileResponse profileResponse)
        {
            var highestLightValue = 0;

            var highlestLightWeapons = new Dictionary<uint, int>()
            {
                { DefinitionHashes.InventoryBuckets.KineticWeapons, 0 },
                { DefinitionHashes.InventoryBuckets.EnergyWeapons, 0 },
                { DefinitionHashes.InventoryBuckets.PowerWeapons, 0 }
            };

            foreach (
                var (characterId, characterEquipment) in profileResponse.CharacterEquipment.Data
            )
            {
                foreach (var characterEquippedItem in characterEquipment.Items)
                {
                    if (!characterEquippedItem.Item.TryGetDefinition(out var itemDefinition))
                        continue;

                    if (itemDefinition.Inventory is null)
                        continue;

                    if (!itemDefinition.Inventory.BucketType.HasValidHash)
                        continue;

                    var bucketHash = itemDefinition.Inventory.BucketType.Hash.GetValueOrDefault();

                    if (!highlestLightWeapons.ContainsKey(bucketHash))
                        continue;

                    var instance = profileResponse.ItemComponents.Instances.Data[
                        characterEquippedItem.ItemInstanceId.GetValueOrDefault()
                    ];

                    var lightValue = instance.PrimaryStat is not null
                        ? instance.PrimaryStat.Value
                        : instance.ItemLevel * 10 + instance.Quality;

                    var currentValue = highlestLightWeapons[bucketHash];

                    if (currentValue < lightValue)
                    {
                        highlestLightWeapons[bucketHash] = lightValue;
                    }
                }
            }

            foreach (var (characterId, characterItems) in profileResponse.CharacterInventories.Data)
            {
                var characterClassType = profileResponse.Characters.Data[characterId].ClassType;
                var characterEquipment = profileResponse.CharacterEquipment.Data[characterId];

                var highlestLightArmorInBucket = new Dictionary<uint, int>()
                {
                    { DefinitionHashes.InventoryBuckets.Helmet, 0 },
                    { DefinitionHashes.InventoryBuckets.Gauntlets, 0 },
                    { DefinitionHashes.InventoryBuckets.ChestArmor, 0 },
                    { DefinitionHashes.InventoryBuckets.LegArmor, 0 },
                    { DefinitionHashes.InventoryBuckets.ClassArmor, 0 }
                };

                foreach (var item in characterItems.Items)
                {
                    if (!item.Item.TryGetDefinition(out var itemDefinition))
                        continue;

                    if (itemDefinition.Inventory is null)
                        continue;

                    if (!itemDefinition.Inventory.BucketType.HasValidHash)
                        continue;

                    var bucketHash = itemDefinition.Inventory.BucketType.Hash.GetValueOrDefault();

                    if (highlestLightArmorInBucket.TryGetValue(bucketHash, out var currentValue))
                    {
                        var instance = profileResponse.ItemComponents.Instances.Data[
                            item.ItemInstanceId.GetValueOrDefault()
                        ];

                        if (instance.PrimaryStat is null)
                            continue;

                        var lightValue = instance.PrimaryStat is not null
                            ? instance.PrimaryStat.Value
                            : instance.ItemLevel * 10 + instance.Quality;

                        if (currentValue < lightValue)
                        {
                            highlestLightArmorInBucket[bucketHash] = lightValue;
                        }
                    }
                    else if (
                        highlestLightWeapons.TryGetValue(bucketHash, out var currentWeaponValue)
                    )
                    {
                        var instance = profileResponse.ItemComponents.Instances.Data[
                            item.ItemInstanceId.GetValueOrDefault()
                        ];

                        if (instance.PrimaryStat is null)
                            continue;

                        var lightValue = instance.PrimaryStat is not null
                            ? instance.PrimaryStat.Value
                            : instance.ItemLevel * 10 + instance.Quality;

                        if (currentWeaponValue < lightValue)
                        {
                            highlestLightWeapons[bucketHash] = lightValue;
                        }
                    }
                }

                foreach (var profileItem in profileResponse.ProfileInventory.Data.Items)
                {
                    if (!profileItem.Item.TryGetDefinition(out var itemDefinition))
                        continue;

                    if (itemDefinition.Inventory is null)
                        continue;

                    if (!itemDefinition.Inventory.BucketType.HasValidHash)
                        continue;

                    var bucketHash = itemDefinition.Inventory.BucketType.Hash.GetValueOrDefault();

                    if (!highlestLightArmorInBucket.ContainsKey(bucketHash))
                        continue;

                    if (
                        _armorBucketHashes.Contains(bucketHash)
                        && itemDefinition.ClassType != characterClassType
                    )
                        continue;

                    if (highlestLightArmorInBucket.TryGetValue(bucketHash, out var currentValue))
                    {
                        var instance = profileResponse.ItemComponents.Instances.Data[
                            profileItem.ItemInstanceId.GetValueOrDefault()
                        ];

                        if (instance.PrimaryStat is null)
                            continue;

                        var lightValue = instance.PrimaryStat is not null
                            ? instance.PrimaryStat.Value
                            : instance.ItemLevel * 10 + instance.Quality;

                        if (currentValue < lightValue)
                        {
                            highlestLightArmorInBucket[bucketHash] = lightValue;
                        }
                    }
                    else if (
                        highlestLightWeapons.TryGetValue(bucketHash, out var currentWeaponValue)
                    )
                    {
                        var instance = profileResponse.ItemComponents.Instances.Data[
                            profileItem.ItemInstanceId.GetValueOrDefault()
                        ];

                        if (instance.PrimaryStat is null)
                            continue;

                        var lightValue = instance.PrimaryStat is not null
                            ? instance.PrimaryStat.Value
                            : instance.ItemLevel * 10 + instance.Quality;

                        if (currentWeaponValue < lightValue)
                        {
                            highlestLightWeapons[bucketHash] = lightValue;
                        }
                    }
                }

                foreach (var characterEquippedItem in characterEquipment.Items)
                {
                    if (!characterEquippedItem.Item.TryGetDefinition(out var itemDefinition))
                        continue;

                    if (itemDefinition.Inventory is null)
                        continue;

                    if (!itemDefinition.Inventory.BucketType.HasValidHash)
                        continue;

                    var bucketHash = itemDefinition.Inventory.BucketType.Hash.GetValueOrDefault();

                    if (highlestLightArmorInBucket.TryGetValue(bucketHash, out var currentValue))
                    {
                        var instance = profileResponse.ItemComponents.Instances.Data[
                            characterEquippedItem.ItemInstanceId.GetValueOrDefault()
                        ];

                        if (instance.PrimaryStat is null)
                            continue;

                        var lightValue = instance.PrimaryStat is not null
                            ? instance.PrimaryStat.Value
                            : instance.ItemLevel * 10 + instance.Quality;

                        if (currentValue < lightValue)
                        {
                            highlestLightArmorInBucket[bucketHash] = lightValue;
                        }
                    }
                    else if (
                        highlestLightWeapons.TryGetValue(bucketHash, out var currentWeaponValue)
                    )
                    {
                        var instance = profileResponse.ItemComponents.Instances.Data[
                            characterEquippedItem.ItemInstanceId.GetValueOrDefault()
                        ];

                        if (instance.PrimaryStat is null)
                            continue;

                        var lightValue = instance.PrimaryStat is not null
                            ? instance.PrimaryStat.Value
                            : instance.ItemLevel * 10 + instance.Quality;

                        if (currentWeaponValue < lightValue)
                        {
                            highlestLightWeapons[bucketHash] = lightValue;
                        }
                    }
                }

                var totalLight =
                    highlestLightArmorInBucket.Sum(x => x.Value)
                    + highlestLightWeapons.Sum(x => x.Value);

                var medianLight = (int)Math.Floor(totalLight / (double)8);

                if (highestLightValue < medianLight)
                {
                    highestLightValue = medianLight;
                }
            }

            return highestLightValue;
        }
    }
}
