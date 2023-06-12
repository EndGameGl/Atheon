using DotNetBungieAPI.Models;
using System.Globalization;

namespace Atheon.Services.Interfaces;

public interface ILocalizationService
{
    ValueTask<BungieLocales> GetGuildLocaleCachedAsync(ulong guildId);
    CultureInfo GetCultureForLocale(BungieLocales locale);
    string GetLocalizedText(string textId, BungieLocales locale, Func<string> fallback);
}
