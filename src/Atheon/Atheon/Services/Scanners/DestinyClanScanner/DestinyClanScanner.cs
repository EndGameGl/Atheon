using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.EventBus;
using Atheon.Services.Scanners.Entities;

namespace Atheon.Services.Scanners.DestinyClanScanner;

public class DestinyClanScanner : EntityScannerBase<DestinyClanScannerInput, DestinyClanScannerContext>
{
    public DestinyClanScanner(
        ILogger<DestinyClanScanner> logger,
        IEventBus<ClanBroadcastDbModel> broadcastEventChannel) : base(logger)
    {
    }
}
