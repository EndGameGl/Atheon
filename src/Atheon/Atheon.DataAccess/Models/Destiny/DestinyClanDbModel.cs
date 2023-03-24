using Atheon.DataAccess.Attributes;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.GroupsV2;

namespace Atheon.DataAccess.Models.Destiny;

[DapperAutomap]
[AutoTable("Clans")]
public class DestinyClanDbModel
{
    [AutoColumn(nameof(ClanId), isPrimaryKey: true, notNull: true)]
    public long ClanId { get; set; }

    [AutoColumn(nameof(ClanName))]
    public string ClanName { get; set; }

    [AutoColumn(nameof(ClanCallsign))]
    public string ClanCallsign { get; set; }

    [AutoColumn(nameof(ClanLevel))]
    public int ClanLevel { get; set; }

    [AutoColumn(nameof(MemberCount))]
    public int MemberCount { get; set; }

    [AutoColumn(nameof(MembersOnline))]
    public int MembersOnline { get; set; }

    [AutoColumn(nameof(IsTracking))]
    public bool IsTracking { get; set; }

    [AutoColumn(nameof(JoinedOn))]
    public DateTime JoinedOn { get; set; }

    [AutoColumn(nameof(LastScan))]
    public DateTime? LastScan { get; set; }

    [AutoColumn(nameof(ShouldRescan))]
    public bool? ShouldRescan { get; set; }

    public static DestinyClanDbModel CreateFromApiResponse(GroupResponse groupResponse)
    {
        var model = new DestinyClanDbModel()
        {
            ClanId = groupResponse.Detail.GroupId,
            ClanName = groupResponse.Detail.Name,
            ClanCallsign = groupResponse.Detail.ClanInfo.ClanCallSign,
            ClanLevel = groupResponse.Detail.ClanInfo.D2ClanProgressions[DefinitionHashes.Progressions.ClanLevel].Level,
            MemberCount = groupResponse.Detail.MemberCount,
            MembersOnline = 0,
            IsTracking = false,
            JoinedOn = DateTime.UtcNow,
            LastScan = null
        };

        return model;
    }
}
