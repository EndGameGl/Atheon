using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Destiny2.Metadata;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
	public class ComputedDataUpdater : IProfileUpdater
	{
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
			dbModel.ComputedData!.Drystreaks ??= new Dictionary<uint, int>();
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

			dbModel.ComputedData.Titles ??= new Dictionary<uint, int>();
			var titleHashes = await _destinyDefinitionDataService.GetTitleHashesCachedAsync();
			var profileRecords = profileResponse.ProfileRecords.Data.Records;
			foreach (var (titleHash, gildHash) in titleHashes)
			{
				var completions = 0;
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

			bool failedToGetInventoryCalculations = true;
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
					failedToGetInventoryCalculations = false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Failed to calculate light based on inventory: Name: {Name}, Type: {Type}, Id: {Id}",
					dbModel.Name,
					dbModel.MembershipType,
					dbModel.MembershipId
				);
			}

			if (
				failedToGetInventoryCalculations
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

			dbModel.ComputedData.ArtifactPowerLevel = profileResponse
				.ProfileProgression
				.Data
				.SeasonalArtifact
				.PowerBonus;

			dbModel.ComputedData.LifetimeScore = profileResponse.ProfileRecords.Data.LifetimeScore;
			dbModel.ComputedData.ActiveScore = profileResponse.ProfileRecords.Data.ActiveScore;
			dbModel.ComputedData.LegacyScore = profileResponse.ProfileRecords.Data.LegacyScore;
			dbModel.ComputedData.TotalTitlesEarned = dbModel.ComputedData.Titles.Count(
				x => x.Value > 0
			);
		}

		private int CalculateHighestLightBasedOnInventory(DestinyProfileResponse profileResponse)
		{
			if (profileResponse.Profile.Data.UserInfo.MembershipId == 4611686018429871637) { }

			var highestLightValue = 0;

			var highestLightWeapons = new Dictionary<uint, int>()
			{
				{ DefinitionHashes.InventoryBuckets.KineticWeapons, 0 },
				{ DefinitionHashes.InventoryBuckets.EnergyWeapons, 0 },
				{ DefinitionHashes.InventoryBuckets.PowerWeapons, 0 }
			};

			foreach (
				var (characterId, characterEquipment) in profileResponse.CharacterEquipment.Data
			)
			{
				foreach (var item in characterEquipment.Items)
				{
					if (!TryGetItemAndBucketHash(item, out var itemDefinition, out var bucketHash))
						continue;

					if (!highestLightWeapons.ContainsKey(bucketHash))
						continue;

					if (!TryGetItemInstance(profileResponse, item, out var instance))
						continue;

					ReassignLightValues(instance, highestLightWeapons, bucketHash);
				}
			}

			foreach (var (characterId, characterItems) in profileResponse.CharacterInventories.Data)
			{
				var characterClassType = profileResponse.Characters.Data[characterId].ClassType;
				var characterEquipment = profileResponse.CharacterEquipment.Data[characterId];

				var highestLightArmorInBucket = new Dictionary<uint, int>()
				{
					{ DefinitionHashes.InventoryBuckets.Helmet, 0 },
					{ DefinitionHashes.InventoryBuckets.Gauntlets, 0 },
					{ DefinitionHashes.InventoryBuckets.ChestArmor, 0 },
					{ DefinitionHashes.InventoryBuckets.LegArmor, 0 },
					{ DefinitionHashes.InventoryBuckets.ClassArmor, 0 }
				};

				foreach (var item in characterItems.Items)
				{
					if (!TryGetItemAndBucketHash(item, out var itemDefinition, out var bucketHash))
						continue;

                    CalculatePowerLevelForItemAndAssign(
                        highestLightArmorInBucket,
                        highestLightWeapons,
                        bucketHash,
                        profileResponse,
                        item);
                }

				foreach (var item in profileResponse.ProfileInventory.Data.Items)
				{
					if (!TryGetItemAndBucketHash(item, out var itemDefinition, out var bucketHash))
						continue;

					if (!highestLightArmorInBucket.ContainsKey(bucketHash))
						continue;

					if (
						_armorBucketHashes.Contains(bucketHash)
						&& itemDefinition.ClassType != characterClassType
					)
						continue;

                    CalculatePowerLevelForItemAndAssign(
                        highestLightArmorInBucket,
                        highestLightWeapons,
                        bucketHash,
                        profileResponse,
                        item);
                }

				foreach (var item in characterEquipment.Items)
				{
					if (!TryGetItemAndBucketHash(item, out var itemDefinition, out var bucketHash))
						continue;

					CalculatePowerLevelForItemAndAssign(
						highestLightArmorInBucket,
                        highestLightWeapons,
						bucketHash,
						profileResponse,
						item);
				}

				var totalLight =
					highestLightArmorInBucket.Sum(x => x.Value)
					+ highestLightWeapons.Sum(x => x.Value);

				var medianLight = (int)Math.Floor(totalLight / (double)8);

				if (highestLightValue < medianLight)
				{
					highestLightValue = medianLight;
				}
			}

			return highestLightValue;
		}

		private static int GetItemPowerLevel(DestinyItemInstanceComponent itemInstance)
		{
			return itemInstance.PrimaryStat is not null
				? itemInstance.PrimaryStat.Value
				: itemInstance.ItemLevel * 10 + itemInstance.Quality;
		}

		private static bool TryGetItemAndBucketHash(
			DestinyItemComponent item,
			out DestinyInventoryItemDefinition itemDefinition,
			out uint bucketHash
		)
		{
			bucketHash = 0;

			if (!item.Item.TryGetDefinition(out itemDefinition!))
				return false;

			if (itemDefinition.Inventory is null)
				return false;

			if (!itemDefinition.Inventory.BucketType.HasValidHash)
				return false;

			bucketHash = itemDefinition.Inventory.BucketType.Hash.GetValueOrDefault();
			return bucketHash > 0;
		}

		private static void ReassignLightValues(
			DestinyItemInstanceComponent itemInstance,
			Dictionary<uint, int> lightValuesByBucket,
			uint bucketHash
		)
		{
			var lightValue = GetItemPowerLevel(itemInstance);

			var currentValue = lightValuesByBucket[bucketHash];

			if (currentValue < lightValue)
			{
				lightValuesByBucket[bucketHash] = lightValue;
			}
		}

		private static bool TryGetItemInstance(
			DestinyProfileResponse profileResponse,
			DestinyItemComponent itemComponent,
			out DestinyItemInstanceComponent destinyItemInstance
		)
		{
			return profileResponse.ItemComponents.Instances.Data.TryGetValue(
				itemComponent.ItemInstanceId.GetValueOrDefault(),
				out destinyItemInstance!
			);
		}

		private static void CalculatePowerLevelForItemAndAssign(
			Dictionary<uint, int> armorBucketValues,
			Dictionary<uint, int> weaponBucketValues,
			uint bucketHash,
			DestinyProfileResponse profileResponse,
			DestinyItemComponent item
		)
		{
			if (armorBucketValues.TryGetValue(bucketHash, out var currentValue))
			{
				if (!TryGetItemInstance(profileResponse, item, out var instance))
					return;

				var lightValue = GetItemPowerLevel(instance);

				if (currentValue < lightValue)
				{
					armorBucketValues[bucketHash] = lightValue;
				}
			}
			else if (weaponBucketValues.TryGetValue(bucketHash, out var currentWeaponValue))
			{
				if (!TryGetItemInstance(profileResponse, item, out var instance))
					return;

				var lightValue = GetItemPowerLevel(instance);

				if (currentWeaponValue < lightValue)
				{
					weaponBucketValues[bucketHash] = lightValue;
				}
			}
		}
	}
}
