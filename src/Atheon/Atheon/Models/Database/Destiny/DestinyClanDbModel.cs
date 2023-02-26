using Atheon.Attributes;
using Atheon.Options;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.GroupsV2;

namespace Atheon.Models.Database.Destiny;

[DapperAutomap]
[AutoTable("Clans")]
public class DestinyClanDbModel
{
    [AutoColumn(nameof(ClanId), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public long ClanId { get; set; }

    [AutoColumn(nameof(ClanName), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string ClanName { get; set; }

    [AutoColumn(nameof(ClanCallsign), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string ClanCallsign { get; set; }

    [AutoColumn(nameof(ClanLevel), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.DEFAULT_VALUE)]
    public int ClanLevel { get; set; }

    [AutoColumn(nameof(MemberCount), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.DEFAULT_VALUE)]
    public int MemberCount { get; set; }

    [AutoColumn(nameof(MembersOnline), sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.DEFAULT_VALUE)]
    public int MembersOnline { get; set; }

    [AutoColumn(nameof(IsTracking), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.BOOLEAN)]
    public bool IsTracking { get; set; }

    [AutoColumn(nameof(JoinedOn), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime JoinedOn { get; set; }

    [AutoColumn(nameof(LastScan), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.DATETIME)]
    public DateTime? LastScan { get; set; }

    [AutoColumn(nameof(ShouldRescan), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.BOOLEAN)]
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
