using Atheon.Models.DiscordModels;
using DotNetBungieAPI.Models;

namespace Atheon.Extensions;

public static class EnumExtensions
{
    public static BungieLocales ConvertToBungieLocale(this DiscordDestinyLanguageEnum discordDestinyLanguage)
    {
        return discordDestinyLanguage switch
        {
            DiscordDestinyLanguageEnum.English => BungieLocales.EN,
            DiscordDestinyLanguageEnum.Russian => BungieLocales.RU,
            DiscordDestinyLanguageEnum.German => BungieLocales.DE,
            DiscordDestinyLanguageEnum.Spanish => BungieLocales.ES,
            DiscordDestinyLanguageEnum.SpanishMexico => BungieLocales.ES_MX,
            DiscordDestinyLanguageEnum.French => BungieLocales.FR,
            DiscordDestinyLanguageEnum.Italian => BungieLocales.IT,
            DiscordDestinyLanguageEnum.Japanese => BungieLocales.JA,
            DiscordDestinyLanguageEnum.Korean => BungieLocales.KO,
            DiscordDestinyLanguageEnum.Polish => BungieLocales.PL,
            DiscordDestinyLanguageEnum.PortugueseBrazil => BungieLocales.PT_BR,
            DiscordDestinyLanguageEnum.ChineseSimplified => BungieLocales.ZH_CHS,
            DiscordDestinyLanguageEnum.ChineseTraditional => BungieLocales.ZH_CHT,
            _ => BungieLocales.EN
        };
    }
}
