using Atheon.Models.Scanning;
using Atheon.Services.Scanners.DestinyClanScanner;
using Atheon.Services.Scanners.DestinyProfileScanner;
using DotNetBungieAPI.Models.GroupsV2;

namespace Atheon.Services.Interfaces
{
    public interface IUserQueue
    {
        Task<ClanScanProgress> EnqueueAndWaitForBroadcastedUserScans(
            DestinyClanScannerContext input,
            CancellationToken cancellationToken);

        Task<ClanScanProgress> EnqueueAndWaitForSilentUserScans(
            DestinyClanScannerContext input,
            CancellationToken cancellationToken);

    }
}
