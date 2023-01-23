using Atheon.Models.Collections;
using Atheon.Services.Interfaces;

namespace Atheon.Services;

public class SqliteClansToScanProvider : IClansToScanProvider
{
    private readonly IDestinyDb _destinyDb;

    private UniqueConcurrentQueue<long> _clanIds;
    private List<long> EmptyList;

    public SqliteClansToScanProvider(
        IDestinyDb destinyDb)
    {
        _destinyDb = destinyDb;
        _clanIds = new UniqueConcurrentQueue<long>(); 
        EmptyList = Array.Empty<long>().ToList();
    }

    public async ValueTask<List<long>> GetClansToScanAsync(int maxAmount)
    {
        if (_clanIds.Count > 0)
        {
            var result = _clanIds.DequeueUpTo(maxAmount).ToList();
            return result;
        }

        if (await TryLoadClanIds())
        {
            var result = _clanIds.DequeueUpTo(maxAmount).ToList();
            return result;
        }

        return EmptyList;
    }

    private async Task<bool> TryLoadClanIds()
    {
        var ids = await _destinyDb.GetClanIdsAsync(true);

        _clanIds.EnqueueRange(ids);

        return _clanIds.Count > 0;
    }
}
