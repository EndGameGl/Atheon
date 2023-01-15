using Atheon.Services.Scanners.DestinyClanScanner;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.GroupsV2;

namespace Atheon.Services.Scanners.DestinyProfileScanner;

public class DestinyProfileScannerInput
{
    public GroupMember GroupMember { get; init; }
    public DestinyClanScannerContext ClanScannerContext { get; init; }
}
