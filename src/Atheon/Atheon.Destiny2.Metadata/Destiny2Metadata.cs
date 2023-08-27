using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny;
using System.Collections.ObjectModel;

namespace Atheon.Destiny2.Metadata;

public static class Destiny2Metadata
{
	public static HashSet<uint> RaidCompletionMetricHashes { get; } =
		new()
		{
			DefinitionHashes.Metrics.LeviathanCompletions,
			DefinitionHashes.Metrics.LeviathanPrestigeCompletions,
			DefinitionHashes.Metrics.EaterofWorldsCompletions,
			DefinitionHashes.Metrics.EaterofWorldsPrestigeRuns,
			DefinitionHashes.Metrics.SpireofStarsCompletions,
			DefinitionHashes.Metrics.SpireofStarsPrestigeRuns,
			DefinitionHashes.Metrics.LastWishCompletions,
			DefinitionHashes.Metrics.ScourgeofthePastCompletions,
			DefinitionHashes.Metrics.CrownofSorrowCompletions,
			DefinitionHashes.Metrics.GardenofSalvationCompletions,
			DefinitionHashes.Metrics.DeepStoneCryptCompletions,
			DefinitionHashes.Metrics.VaultofGlassCompletions,
			DefinitionHashes.Metrics.VowoftheDiscipleCompletions,
			DefinitionHashes.Metrics.KingsFallCompletions,
			DefinitionHashes.Metrics.RootofNightmaresCompletions
		};

	public static ReadOnlyDictionary<uint, uint> DryStreakItemSettings { get; } =
		new(
			new Dictionary<uint, uint>
			{
				{
					DefinitionHashes.Collectibles.OneThousandVoices,
					DefinitionHashes.Metrics.LastWishCompletions
				},
				{
					DefinitionHashes.Collectibles.EyesofTomorrow,
					DefinitionHashes.Metrics.DeepStoneCryptCompletions
				},
				{
					DefinitionHashes.Collectibles.VexMythoclast,
					DefinitionHashes.Metrics.VaultofGlassCompletions
				},
				{
					DefinitionHashes.Collectibles.CollectiveObligation,
					DefinitionHashes.Metrics.VowoftheDiscipleCompletions
				},
				{
					DefinitionHashes.Collectibles.Heartshadow,
					DefinitionHashes.Metrics.DualityCompletions
				},
				{
					DefinitionHashes.Collectibles.InMemoriamShell,
					DefinitionHashes.Metrics.Wins_1365664208
				},
				{
					DefinitionHashes.Collectibles.TouchofMalice,
					DefinitionHashes.Metrics.KingsFallCompletions
				},
				{
					DefinitionHashes.Collectibles.HierarchyofNeeds,
					DefinitionHashes.Metrics.SpireoftheWatcherCompletions
				},
				{
					DefinitionHashes.Collectibles.ConditionalFinality,
					DefinitionHashes.Metrics.RootofNightmaresCompletions
				},
				{
					DefinitionHashes.Collectibles.TheNavigator,
					DefinitionHashes.Metrics.GhostsoftheDeepCompletions
				}
			}
		);

	public static ReadOnlyDictionary<uint, string> DryStreakItemSources { get; } =
		new(
			new Dictionary<uint, string>
			{
				{ DefinitionHashes.Collectibles.OneThousandVoices, "Source: Last Wish Raid" },
				{ DefinitionHashes.Collectibles.EyesofTomorrow, "Source: Deep Stone Crypt Raid" },
				{ DefinitionHashes.Collectibles.VexMythoclast, "Source: Vault of Glass Raid" },
				{
					DefinitionHashes.Collectibles.CollectiveObligation,
					"Source: Vow of the Disciple Raid"
				},
				{ DefinitionHashes.Collectibles.Heartshadow, "Source: Duality Dungeon" },
				{ DefinitionHashes.Collectibles.InMemoriamShell, "Source: Trials of Osiris" },
				{ DefinitionHashes.Collectibles.TouchofMalice, "Source: Kings Fall Raid" },
				{
					DefinitionHashes.Collectibles.HierarchyofNeeds,
					"Source: Spire of the Watcher Dungeon"
				},
				{
					DefinitionHashes.Collectibles.ConditionalFinality,
					"Source: Root of Nightmares Raid"
				},
				{ DefinitionHashes.Collectibles.TheNavigator, "Source: Ghosts of the Deep Dungeon" }
			}
		);

	public static DestinyComponentType[] GenericProfileComponents { get; } =
		{
			DestinyComponentType.Profiles,
			DestinyComponentType.ProfileInventories,
			DestinyComponentType.ProfileProgression,
			DestinyComponentType.Characters,
			DestinyComponentType.CharacterProgressions,
			DestinyComponentType.CharacterActivities,
			DestinyComponentType.CharacterInventories,
			DestinyComponentType.CharacterEquipment,
			DestinyComponentType.ItemInstances,
			DestinyComponentType.Collectibles,
			DestinyComponentType.Records,
			DestinyComponentType.Metrics
		};
}
