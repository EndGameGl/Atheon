using Discord.Interactions;

namespace Atheon.DataAccess.Models.Discord;

public enum DiscordDestinyLanguageEnum
{
    [ChoiceDisplay("English")]
    English,

    [ChoiceDisplay("Russian")]
    Russian,

    [ChoiceDisplay("German")]
    German,

    [ChoiceDisplay("Spanish")]
    Spanish,

    [ChoiceDisplay("Spanish (Mexico)")]
    SpanishMexico,

    [ChoiceDisplay("French")]
    French,

    [ChoiceDisplay("Italian")]
    Italian,

    [ChoiceDisplay("Japanese")]
    Japanese,

    [ChoiceDisplay("Korean")]
    Korean,

    [ChoiceDisplay("Polish")]
    Polish,

    [ChoiceDisplay("Portuguese (Brazil)")]
    PortugueseBrazil,

    [ChoiceDisplay("Chinese (Simplified)")]
    ChineseSimplified,

    [ChoiceDisplay("Chinese (Traditional)")]
    ChineseTraditional
}
