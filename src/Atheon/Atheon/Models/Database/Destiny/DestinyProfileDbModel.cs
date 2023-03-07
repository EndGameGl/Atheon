using Atheon.Attributes;
using Atheon.Extensions;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Options;
using Atheon.Services.BungieApi;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.Destiny.Milestones;
using DotNetBungieAPI.Models.Destiny.Progressions;
using DotNetBungieAPI.Models.Destiny.Quests;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Models.Database.Destiny;

[AutoTable("DestinyProfiles")]
[DapperAutomap]
public class DestinyProfileDbModel
{
    [AutoColumn(nameof(MembershipId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long MembershipId { get; set; }

    [AutoColumn(nameof(MembershipType), notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.INT)]
    public BungieMembershipType MembershipType { get; set; }

    [AutoColumn(nameof(ClanId), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long? ClanId { get; set; }

    [AutoColumn(nameof(Name), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string? Name { get; set; }

    [AutoColumn(nameof(DateLastPlayed), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime DateLastPlayed { get; set; }

    [AutoColumn(nameof(MinutesPlayedTotal), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long MinutesPlayedTotal { get; set; }

    [AutoColumn(nameof(Collectibles), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public HashSet<uint> Collectibles { get; set; } = new();

    [AutoColumn(nameof(Records), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public Dictionary<uint, DestinyRecordDbModel> Records { get; set; } = new();

    [AutoColumn(nameof(Progressions), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public Dictionary<uint, DestinyProgressionDbModel> Progressions { get; set; } = new();

    [AutoColumn(nameof(ResponseMintedTimestamp), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime? ResponseMintedTimestamp { get; set; }

    [AutoColumn(nameof(LastUpdated), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    ///     Components to check: PresentationNodes, Records, Collectibles, Metrics, StringVariables, Craftables, Transitory
    /// </summary>
    [AutoColumn(nameof(SecondaryComponentsMintedTimestamp), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime? SecondaryComponentsMintedTimestamp { get; set; }

    [AutoColumn(nameof(ComputedData), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public DestinyComputedData? ComputedData { get; set; } = new();

    public static async Task<DestinyProfileDbModel> CreateFromApiResponse(
        long clanId,
        DestinyProfileResponse destinyProfileResponse,
        IBungieClient bungieClient,
        DestinyDefinitionDataService destinyDefinitionDataService)
    {
        var userInfo = destinyProfileResponse.Profile.Data.UserInfo;

        var dbModel = new DestinyProfileDbModel()
        {
            MembershipId = userInfo.MembershipId,
            MembershipType = userInfo.MembershipType,
            Name = $"{userInfo.BungieGlobalDisplayName}#{userInfo.BungieGlobalDisplayNameCode:D4}",
            DateLastPlayed = destinyProfileResponse.Profile.Data.DateLastPlayed,
            MinutesPlayedTotal = destinyProfileResponse.Characters.Data.Sum(x => x.Value.MinutesPlayedTotal),
            Collectibles = new HashSet<uint>(),
            Records = new Dictionary<uint, DestinyRecordDbModel>(),
            ClanId = clanId,
            ResponseMintedTimestamp = destinyProfileResponse.ResponseMintedTimestamp,
            SecondaryComponentsMintedTimestamp = destinyProfileResponse.SecondaryComponentsMintedTimestamp
        };

        dbModel.FillCollectibles(destinyProfileResponse);
        dbModel.FillRecords(destinyProfileResponse);
        dbModel.FillProgressions(destinyProfileResponse, bungieClient);
        await dbModel.FillComputedData(destinyProfileResponse, destinyDefinitionDataService);
        return dbModel;
    }

    private void FillCollectibles(
        DestinyProfileResponse destinyProfileResponse)
    {
        foreach (var (collectibleHash, collectibleState) in destinyProfileResponse.ProfileCollectibles.Data.Collectibles)
        {
            if (!collectibleState.State.HasFlag(DestinyCollectibleState.NotAcquired))
            {
                Collectibles.Add(collectibleHash.Hash.GetValueOrDefault());
            }
        }

        foreach (var (characterId, collectibles) in destinyProfileResponse.CharacterCollectibles.Data)
        {
            foreach (var (collectibleHash, collectibleState) in collectibles.Collectibles)
            {
                if (!collectibleState.State.HasFlag(DestinyCollectibleState.NotAcquired))
                {
                    Collectibles.Add(collectibleHash.Hash.GetValueOrDefault());
                }
            }
        }
    }

    private void FillRecords(
        DestinyProfileResponse destinyProfileResponse)
    {
        foreach (var (recordHash, recordComponent) in destinyProfileResponse.ProfileRecords.Data.Records)
        {
            var recordDbModel = new DestinyRecordDbModel()
            {
                State = recordComponent.State,
                CompletedCount = recordComponent.CompletedCount,
                Objectives = recordComponent.Objectives.Count > 0 ? ConvertToDbObjectives(recordComponent.Objectives) : null,
                IntervalObjectives = recordComponent.IntervalObjectives.Count > 0 ? ConvertToDbObjectives(recordComponent.IntervalObjectives) : null,
            };

            Records.TryAdd(recordHash, recordDbModel);
        }

        var firstCharacter = destinyProfileResponse.CharacterRecords.Data.First().Value;

        foreach (var (recordHash, _) in firstCharacter.Records)
        {
            var recordComponent = destinyProfileResponse.CharacterRecords.Data.GetOptimalRecordAcrossCharacters(recordHash);

            Records.Add(recordHash, new DestinyRecordDbModel(recordComponent));
        }
    }

    private void FillProgressions(
        DestinyProfileResponse destinyProfileResponse,
        IBungieClient bungieClient)
    {
        if (destinyProfileResponse.CharacterProgressions.Data.Count == 0)
            return;

        var firstCharacter = destinyProfileResponse.CharacterProgressions.Data.First().Value;

        foreach (var (progressionHash, _) in firstCharacter.Progressions)
        {
            var progressionData = destinyProfileResponse.CharacterProgressions.Data.GetMostCompletedProgressionAcrossCharacters(
                progressionHash.Hash!.Value,
                bungieClient);

            Progressions.Add(progressionHash.Hash.Value, new DestinyProgressionDbModel(progressionData));
        }
    }

    private async Task FillComputedData(
        DestinyProfileResponse destinyProfileResponse,
        DestinyDefinitionDataService destinyDefinitionDataService)
    {
        ComputedData!.Drystreaks = new Dictionary<uint, int>();
        foreach (var (collectibleHash, metricHash) in Destiny2Metadata.DryStreakItemSettings)
        {
            if (!destinyProfileResponse.Metrics.Data.Metrics.TryGetValue(metricHash, out var metricComponent))
                continue;

            var progress = metricComponent.ObjectiveProgress.Progress ?? 0;

            if (destinyProfileResponse.ProfileCollectibles.Data.Collectibles.TryGetValue(collectibleHash, out var collectibleComponent) &&
                collectibleComponent.State.HasFlag(DestinyCollectibleState.NotAcquired))
            {
                ComputedData.Drystreaks[collectibleHash] = progress;
                continue;
            }

            foreach (var (_, characterCollectibles) in destinyProfileResponse.CharacterCollectibles.Data)
            {
                if (characterCollectibles.Collectibles.TryGetValue(collectibleHash, out var chacterCollectibleComponent) &&
                    !chacterCollectibleComponent.State.HasFlag(DestinyCollectibleState.NotAcquired))
                {
                    continue;
                }
                else
                {
                    ComputedData.Drystreaks[collectibleHash] = progress;
                    break;
                }
            }
        }

        ComputedData.Titles = new Dictionary<uint, int>();
        var titleHashes = await destinyDefinitionDataService.GetTitleHashesCachedAsync();
        var profileRecords = destinyProfileResponse.ProfileRecords.Data.Records;
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

            ComputedData.Titles[titleHash] = completions;
        }
    }

    private static List<DestinyObjectiveProgressDbModel> ConvertToDbObjectives(IEnumerable<DestinyObjectiveProgress> source)
    {
        return source.Select(x => new DestinyObjectiveProgressDbModel(x)).ToList();
    }
}
