using Atheon.DataAccess.Models.Discord;
using Atheon.Models.DiscordModels;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;

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

    public static DestinyGameVersions ConvertToDestinyGameVersion(this DestinyGameVersionEnum versionEnum)
    {
        return versionEnum switch
        {
            DestinyGameVersionEnum.None => DestinyGameVersions.None,
            DestinyGameVersionEnum.Vanilla => DestinyGameVersions.Vanilla,
            DestinyGameVersionEnum.Osiris => DestinyGameVersions.Osiris,
            DestinyGameVersionEnum.Warmind => DestinyGameVersions.Warmind,
            DestinyGameVersionEnum.Forsaken => DestinyGameVersions.Forsaken,
            DestinyGameVersionEnum.YearTwoAnnualPass => DestinyGameVersions.YearTwoAnnualPass,
            DestinyGameVersionEnum.Shadowkeep => DestinyGameVersions.Shadowkeep,
            DestinyGameVersionEnum.BeyondLight => DestinyGameVersions.BeyondLight,
            DestinyGameVersionEnum.Anniversary30th => DestinyGameVersions.Anniversary30th,
            DestinyGameVersionEnum.TheWitchQueen => DestinyGameVersions.TheWitchQueen,
            DestinyGameVersionEnum.Lightfall => DestinyGameVersions.Lightfall,
            _ => throw new NotImplementedException(),
        };
    }
}
