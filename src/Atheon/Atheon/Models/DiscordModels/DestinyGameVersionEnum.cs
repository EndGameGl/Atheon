using Discord.Interactions;

namespace Atheon.Models.DiscordModels;

public enum DestinyGameVersionEnum
{
    [Hide]
    None = 0,
    [Hide]
    Vanilla = 1,
    [Hide]
    Osiris = 2,
    [Hide]
    Warmind = 4,

    [ChoiceDisplay("Forsaken")]
    Forsaken = 8,

    [ChoiceDisplay("Year Two Annual Pass")]
    YearTwoAnnualPass = 16,

    [ChoiceDisplay("Shadowkeep")]
    Shadowkeep = 32,

    [ChoiceDisplay("Beyond Light")]
    BeyondLight = 64,

    [ChoiceDisplay("Anniversary 30th")]
    Anniversary30th = 128,

    [ChoiceDisplay("The Witch Queen")]
    TheWitchQueen = 256,

    [ChoiceDisplay("Lightfall")]
    Lightfall = 512
}
