using Atheon.DataAccess.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;

namespace Atheon.Services.Scanners.DestinyClanMemberScanner;

public class DestinyClanMemberScannerContext
{
    public DestinyProfileResponse? DestinyProfileResponse { get; set; }
    public DestinyProfileDbModel? ProfileDbModel { get; set; }
}
