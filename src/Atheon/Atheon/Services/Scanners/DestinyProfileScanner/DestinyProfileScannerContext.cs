using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.DestinyProfileScanner;

public class DestinyProfileScannerContext
{
    public DestinyProfileResponse? DestinyProfileResponse { get; set; }
    public IBungieClient? BungieClient { get; set; }
}
