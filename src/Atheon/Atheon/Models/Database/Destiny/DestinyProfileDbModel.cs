using Atheon.Attributes;
using Atheon.Models.Database.Destiny.Profiles;
using Atheon.Options;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;

namespace Atheon.Models.Database.Destiny;

[AutoTable("DestinyProfiles")]
[DapperAutomap]
public class DestinyProfileDbModel
{
    [AutoColumn(nameof(MembershipId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long MembershipId { get; set; }

    [AutoColumn(nameof(MembershipType), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.INT)]
    public BungieMembershipType MembershipType { get; set; }

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

    [AutoColumn(nameof(ResponseMintedTimestamp), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime? ResponseMintedTimestamp { get; set; }

    /// <summary>
    ///     Components to check: PresentationNodes, Records, Collectibles, Metrics, StringVariables, Craftables, Transitory
    /// </summary>
    [AutoColumn(nameof(SecondaryComponentsMintedTimestamp), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime? SecondaryComponentsMintedTimestamp { get; set; }

    public static DestinyProfileDbModel CreateFromApiResponse(
        DestinyProfileResponse destinyProfileResponse)
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

            ResponseMintedTimestamp = destinyProfileResponse.ResponseMintedTimestamp,
            SecondaryComponentsMintedTimestamp = destinyProfileResponse.SecondaryComponentsMintedTimestamp
        };

        dbModel.FillCollectibles(destinyProfileResponse);
        dbModel.FillRecords(destinyProfileResponse);

        return dbModel;
    }

    private void FillCollectibles(DestinyProfileResponse destinyProfileResponse)
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

    private void FillRecords(DestinyProfileResponse destinyProfileResponse)
    {
        foreach (var (recordHash, recordComponent) in destinyProfileResponse.ProfileRecords.Data.Records)
        {
            var recordDbModel = new DestinyRecordDbModel()
            {
                State = recordComponent.State,
                CompletedCount = recordComponent.CompletedCount,
                Objectives = recordComponent.Objectives.Count > 0 ? new List<DestinyObjectiveProgressDbModel>() : null,
                IntervalObjectives = recordComponent.IntervalObjectives.Count > 0 ? new List<DestinyObjectiveProgressDbModel>() : null,
            };

            Records.TryAdd(recordHash, recordDbModel);
        }
    }
}
