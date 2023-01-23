using Atheon.Services.Scanners.DestinyClanScanner;
using DotNetBungieAPI.Models.GroupsV2;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.DestinyClanMemberScanner;

public class DestinyClanMemberScannerInput
{
    public required GroupMember GroupMember { get; init; }
    public required DestinyClanScannerContext ClanScannerContext { get; init; }
    public required IBungieClient BungieClient { get; init; }
}
