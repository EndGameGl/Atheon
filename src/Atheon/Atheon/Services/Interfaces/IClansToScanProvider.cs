namespace Atheon.Services.Interfaces;

public interface IClansToScanProvider
{
    ValueTask<List<long>> GetClansToScanAsync(int maxAmount);
}
