using Atheon.Models.Scanning;
using Atheon.Services.Scanners.DestinyClanScanner;

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
