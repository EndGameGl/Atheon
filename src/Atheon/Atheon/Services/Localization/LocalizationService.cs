using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models;
using System.Globalization;

namespace Atheon.Services.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly IDestinyDb _destinyDb;
    private readonly IMemoryCache _memoryCache;

    private readonly Dictionary<BungieLocales, CultureInfo> _localeToCultureMapping = new()
    {
        [BungieLocales.EN] = CultureInfo.GetCultureInfo("en-US"),
        [BungieLocales.RU] = CultureInfo.GetCultureInfo("ru-RU"),
    };

    public LocalizationService(
        IDestinyDb destinyDb,
        IMemoryCache memoryCache)
    {
        _destinyDb = destinyDb;
        _memoryCache = memoryCache;
    }

    public CultureInfo GetCultureForLocale(BungieLocales locale)
    {
        if (_localeToCultureMapping.TryGetValue(locale, out var culture))
            return culture;
        return SystemDefaults.DefaultCulture;
    }

    public async ValueTask<BungieLocales> GetGuildLocaleCachedAsync(ulong guildId)
    {
        return await _memoryCache.GetOrAddAsync(
            $"guild_lang_{guildId}",
            async () => (await _destinyDb.GetGuildLanguageAsync(guildId)).ConvertToBungieLocale(),
            TimeSpan.FromSeconds(15),
            Caching.CacheExpirationType.Absolute);
    }
}
