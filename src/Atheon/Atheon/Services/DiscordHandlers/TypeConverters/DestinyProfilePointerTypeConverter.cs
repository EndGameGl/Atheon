using Atheon.Models.Destiny;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models;

namespace Atheon.Services.DiscordHandlers.TypeConverters;

public class DestinyProfilePointerTypeConverter : TypeConverter<DestinyProfilePointer>
{
    public override ApplicationCommandOptionType GetDiscordType()
    {
        return ApplicationCommandOptionType.String;
    }

    public override async Task<TypeConverterResult> ReadAsync(
        IInteractionContext context,
        IApplicationCommandInteractionDataOption option,
        IServiceProvider services)
    {
        var value = option.Value.ToString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return TypeConverterResult.FromError(InteractionCommandError.ParseFailed, "Invalid format: empty string");
        }

        if (!value.Contains('-'))
        {
            return TypeConverterResult.FromError(InteractionCommandError.ParseFailed, "Invalid format: missing '-'");
        }

        var values = value.Split('-');

        if (values.Length != 2)
        {
            return TypeConverterResult.FromError(InteractionCommandError.ParseFailed, "Invalid format: more segments than expected");
        }

        if (!long.TryParse(values[0], out var membershipId))
        {
            return TypeConverterResult.FromError(InteractionCommandError.ParseFailed, "Invalid format: failed to parse membership id");
        }

        if (!Enum.TryParse<BungieMembershipType>(values[1], out var membershipType))
        {
            return TypeConverterResult.FromError(InteractionCommandError.ParseFailed, "Invalid format: failed to parse membership type");
        }

        return TypeConverterResult.FromSuccess(new DestinyProfilePointer()
        {
            MembershipId = membershipId,
            MembershipType = membershipType
        });
    }
}
