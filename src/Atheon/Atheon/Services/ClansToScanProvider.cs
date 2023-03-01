using Atheon.Models.Collections;
using Atheon.Services.Interfaces;

namespace Atheon.Services;

public class ClansToScanProvider : IClansToScanProvider
{
    private readonly IDestinyDb _destinyDb;

    private UniqueConcurrentQueue<long> _clanIds;
    private List<long> EmptyList;

    public ClansToScanProvider(
        IDestinyDb destinyDb)
    {
        _destinyDb = destinyDb;
        _clanIds = new UniqueConcurrentQueue<long>();
        EmptyList = Array.Empty<long>().ToList();
    }

    public async ValueTask<List<long>> GetClansToScanAsync(int maxAmount, DateTime olderThan)
    {
        if (_clanIds.Count > 0)
        {
            var result = _clanIds.DequeueUpTo(maxAmount).ToList();
            return result;
        }

        if (await TryLoadClanIds(olderThan))
        {
            var result = _clanIds.DequeueUpTo(maxAmount).ToList();
            return result;
        }

        return EmptyList;
    }

    private async Task<bool> TryLoadClanIds(DateTime olderThan)
    {
        var ids = await _destinyDb.GetClanIdsAsync(true, olderThan);

        _clanIds.EnqueueRange(ids);

        return _clanIds.Count > 0;
    }
}
