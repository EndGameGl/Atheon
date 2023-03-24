using Discord.Interactions;

namespace Atheon.Models.DiscordModels;

public enum DiscordNullableBoolean
{
    [Hide]
    None = -1,

    [ChoiceDisplay("False")]
    False = 0,

    [ChoiceDisplay("True")]
    True = 1
}
