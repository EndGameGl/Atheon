using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models;

namespace Atheon.Services.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly IDestinyDb _destinyDb;
    private readonly IMemoryCache _memoryCache;

    public LocalizationService(
        IDestinyDb destinyDb,
        IMemoryCache memoryCache)
    {
        _destinyDb = destinyDb;
        _memoryCache = memoryCache;
    }

    public async ValueTask<BungieLocales> GetGuildLocale(ulong guildId)
    {
        return await _memoryCache.GetOrAddAsync(
            $"guild_lang_{guildId}",
            async () => (await _destinyDb.GetGuildLanguageAsync(guildId)).ConvertToBungieLocale(),
            TimeSpan.FromSeconds(15),
            Caching.CacheExpirationType.Absolute);
    }
}
