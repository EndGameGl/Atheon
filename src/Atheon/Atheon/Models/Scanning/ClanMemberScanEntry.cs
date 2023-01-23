using Atheon.Services.Scanners.DestinyClanScanner;
using DotNetBungieAPI.Models.GroupsV2;

namespace Atheon.Models.Scanning;

public class ClanMemberScanEntry
{
    public DestinyClanScannerContext UpdateContext { get; init; }
    public GroupMember Member { get; init; }
    public CancellationToken CancellationToken { get; set; }
    public bool Silent { get; init; }
}
