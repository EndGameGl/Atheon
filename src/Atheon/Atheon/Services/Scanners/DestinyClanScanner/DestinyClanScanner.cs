using Atheon.Services.Scanners.Entities;

namespace Atheon.Services.Scanners.DestinyClanScanner;

public class DestinyClanScanner : EntityScannerBase<DestinyClanScannerInput, DestinyClanScannerContext>
{
    public DestinyClanScanner(
        ILogger<DestinyClanScanner> logger) : base(logger)
    {
    }
}
