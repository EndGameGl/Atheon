using Discord.Interactions;

namespace Atheon.DataAccess.Models.Discord;

public enum DiscordTimeType
{
    [ChoiceDisplay("Minutes")]
    Minutes,

    [ChoiceDisplay("Hours")]
    Hours,

    [ChoiceDisplay("Days")]
    Days
}
