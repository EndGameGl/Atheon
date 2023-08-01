using Atheon.DataAccess;
using Atheon.DataAccess.Sqlite;
using Atheon.Extensions;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Atheon.Services.Localization;

public partial class LocalizationService : ILocalizationService
{
    private readonly IDestinyDb _destinyDb;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<LocalizationService> _logger;
    private readonly IGuildDb _guildDb;
    private readonly Dictionary<BungieLocales, CultureInfo> _localeToCultureMapping = new()
    {
        [BungieLocales.EN] = CultureInfo.GetCultureInfo("en-US"),
        [BungieLocales.RU] = CultureInfo.GetCultureInfo("ru-RU"),
    };

    private readonly Dictionary<BungieLocales, Dictionary<string, string>> _localization = new() { };

    public LocalizationService(
        IDestinyDb destinyDb,
        IMemoryCache memoryCache,
        ILogger<LocalizationService> logger,
        IGuildDb guildDb,
        IOptions<JsonOptions> jsonOptions)
    {
        _destinyDb = destinyDb;
        _memoryCache = memoryCache;
        _logger = logger;
        _guildDb = guildDb;
        var serializerOptions = jsonOptions.Value.SerializerOptions;

        var files = Directory.GetFiles("./Localization/");
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var regex = FileNameRegex();
            var match = regex.Match(fileName);
            if (match.Success)
            {
                var localeName = match.Groups["localeName"].Value;
                try
                {
                    var bungieLocale = localeName.ParseLocale();
                    using var fileStream = File.OpenRead(file);
                    var localeData = JsonSerializer.Deserialize<Dictionary<string, string>>(fileStream, serializerOptions) ?? new();
                    _localization[bungieLocale] = localeData;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Attempted to load unknown locale: {LocaleName}", localeName);
                }
            }
        }
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
            async () => (await _guildDb.GetGuildLanguageAsync(guildId)).ConvertToBungieLocale(),
            TimeSpan.FromSeconds(15),
            Caching.CacheExpirationType.Absolute);
    }

    public string GetLocalizedText(string textId, BungieLocales locale, Func<string> fallback)
    {
        if (_localization.TryGetValue(locale, out var phrases) && phrases.TryGetValue(textId, out var text))
        {
            return text;
        }
        else if (TryGetDefaultText(textId, out var textEn))
        {
            return textEn!;
        }
        return fallback();
    }

    private bool TryGetDefaultText(string textId, out string? text)
    {
        text = null;
        if (_localization.TryGetValue(BungieLocales.EN, out var phrases) && phrases.TryGetValue(textId, out text))
        {
            return true;
        }
        return false;
    }

    [GeneratedRegex("text.(?<localeName>[a-z\\-]+).json", RegexOptions.Compiled)]
    private static partial Regex FileNameRegex();
}