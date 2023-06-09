using Atheon.DataAccess.Attributes;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Destiny2.Metadata;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Quests;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.DataAccess.Models.Destiny;

[AutoTable("DestinyProfiles")]
[DapperAutomap]
public class DestinyProfileDbModel
{
    [AutoColumn(nameof(MembershipId), isPrimaryKey: true, notNull: true)]
    public long MembershipId { get; set; }

    [AutoColumn(nameof(MembershipType), notNull: true)]
    public BungieMembershipType MembershipType { get; set; }

    [AutoColumn(nameof(ClanId))]
    public long? ClanId { get; set; }

    [AutoColumn(nameof(Name))]
    public string? Name { get; set; }

    [AutoColumn(nameof(DateLastPlayed))]
    public DateTime DateLastPlayed { get; set; }

    [AutoColumn(nameof(MinutesPlayedTotal))]
    public long MinutesPlayedTotal { get; set; }

    [AutoColumn(nameof(Collectibles))]
    public HashSet<uint> Collectibles { get; set; } = new();

    [AutoColumn(nameof(Records))]
    public Dictionary<uint, DestinyRecordDbModel> Records { get; set; } = new();

    [AutoColumn(nameof(Progressions))]
    public Dictionary<uint, DestinyProgressionDbModel> Progressions { get; set; } = new();

    [AutoColumn(nameof(ResponseMintedTimestamp))]
    public DateTime? ResponseMintedTimestamp { get; set; }

    [AutoColumn(nameof(LastUpdated))]
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    ///     Components to check: PresentationNodes, Records, Collectibles, Metrics, StringVariables, Craftables, Transitory
    /// </summary>
    [AutoColumn(nameof(SecondaryComponentsMintedTimestamp))]
    public DateTime? SecondaryComponentsMintedTimestamp { get; set; }

    [AutoColumn(nameof(ComputedData))]
    public DestinyComputedData? ComputedData { get; set; } = new();

    [AutoColumn(nameof(Metrics))]
    public Dictionary<uint, DestinyMetricDbModel> Metrics { get; set; } = new();

    [AutoColumn(nameof(CurrentGuardianRank))]
    public int CurrentGuardianRank { get; set; }

    [AutoColumn(nameof(CurrentActivityData))]
    public PlayerActivityData? CurrentActivityData { get; set; }

    [AutoColumn(nameof(GameVersionsOwned))]
    public DestinyGameVersions GameVersionsOwned { get; set; }


    public static async Task<DestinyProfileDbModel> CreateFromApiResponse(
        long clanId,
        DestinyProfileResponse destinyProfileResponse,
        IBungieClient bungieClient,
        List<(uint TitleRecordHash, uint? TitleGildRecordHash)> titleHashes)
    {
        var userInfo = destinyProfileResponse.Profile.Data.UserInfo;

        var lastPlayedCharacterId = destinyProfileResponse.GetLastPlayedCharacterId();

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
            SecondaryComponentsMintedTimestamp = destinyProfileResponse.SecondaryComponentsMintedTimestamp,
            CurrentGuardianRank = destinyProfileResponse.Profile.Data.CurrentGuardianRank,
            GameVersionsOwned = destinyProfileResponse.Profile.Data.VersionsOwned
        };

        if (lastPlayedCharacterId.HasValue)
        {
            var characterActivities = destinyProfileResponse.CharacterActivities.Data[lastPlayedCharacterId.Value];
            dbModel.CurrentActivityData = new PlayerActivityData()
            {
                ActivityHash = characterActivities.CurrentActivity.Hash,
                ActivityModeHashes = characterActivities.CurrentActivityModes.Select(x => x.Hash.GetValueOrDefault()).Distinct().ToList(),
                PlaylistActivityHash = characterActivities.CurrentPlaylistActivity.Hash,
                DateActivityStarted = characterActivities.DateActivityStarted,
                ActivityModeHash = characterActivities.CurrentActivityMode.Hash
            };
        }

        dbModel.FillCollectibles(destinyProfileResponse);
        dbModel.FillRecords(destinyProfileResponse);
        dbModel.FillProgressions(destinyProfileResponse, bungieClient);
        dbModel.FillMetrics(destinyProfileResponse);
        await dbModel.FillComputedData(destinyProfileResponse, titleHashes);
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

    private void FillMetrics(
        DestinyProfileResponse destinyProfileResponse)
    {
        foreach (var (metricHash, metricComponent) in destinyProfileResponse.Metrics.Data.Metrics)
        {
            if (metricComponent.ObjectiveProgress is null)
                continue;

            Metrics.TryAdd(metricHash, DestinyMetricDbModel.FromMetricComponent(metricComponent));
        }
    }

    private async Task FillComputedData(
        DestinyProfileResponse destinyProfileResponse,
        List<(uint TitleRecordHash, uint? TitleGildRecordHash)> titleHashes)
    {
        ComputedData!.Drystreaks = new Dictionary<uint, int>();
        foreach (var (collectibleHash, metricHash) in Destiny2Metadata.DryStreakItemSettings)
        {
            if (!destinyProfileResponse.Metrics.Data.Metrics.TryGetValue(metricHash, out var metricComponent))
                continue;

            var progress = metricComponent.ObjectiveProgress.Progress ?? 0;

            if (!Collectibles.Contains(collectibleHash))
            {
                ComputedData.Drystreaks[collectibleHash] = progress;
            }
        }

        ComputedData.Titles = new Dictionary<uint, int>();
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
                    completions += gildRecordComponent.CompletedCount ?? 0;
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
