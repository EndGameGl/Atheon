using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny;
using System.Collections.ObjectModel;

namespace Atheon;

public static class Destiny2Metadata
{
    public static HashSet<uint> RaidCompletionMetricHashes { get; } = new()
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
        DefinitionHashes.Metrics.KingsFallCompletions
    };

    public static ReadOnlyDictionary<uint, uint> DryStreakItemSettings { get; } = new(new Dictionary<uint, uint>
    {
        { DefinitionHashes.Collectibles.OneThousandVoices, DefinitionHashes.Metrics.LastWishCompletions },
        { DefinitionHashes.Collectibles.EyesofTomorrow, DefinitionHashes.Metrics.DeepStoneCryptCompletions },
        { DefinitionHashes.Collectibles.VexMythoclast, DefinitionHashes.Metrics.VaultofGlassCompletions },
        { DefinitionHashes.Collectibles.CollectiveObligation, DefinitionHashes.Metrics.VowoftheDiscipleCompletions },
        { DefinitionHashes.Collectibles.Heartshadow, DefinitionHashes.Metrics.DualityCompletions },
        { DefinitionHashes.Collectibles.InMemoriamShell, DefinitionHashes.Metrics.Wins_1365664208 },
        { DefinitionHashes.Collectibles.TouchofMalice, DefinitionHashes.Metrics.KingsFallCompletions },
        { DefinitionHashes.Collectibles.HierarchyofNeeds, DefinitionHashes.Metrics.SpireoftheWatcherCompletions }
    });

    public static DestinyComponentType[] GenericProfileComponents { get; } =
    {
        DestinyComponentType.Profiles,
        DestinyComponentType.Characters,
        DestinyComponentType.CharacterProgressions,
        DestinyComponentType.CharacterActivities,
        DestinyComponentType.Collectibles,
        DestinyComponentType.Records,
        DestinyComponentType.Metrics
    };

}
