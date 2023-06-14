using Atheon.DataAccess;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Preconditions;

public class AtheonBotAdminOrOwnerAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        if (context.Client.TokenType == TokenType.Bot)
        {
            IApplication application = await context.Client.GetApplicationInfoAsync();
            if (context.User.Id == application.Owner.Id)
            {
                return PreconditionResult.FromSuccess();
            }
        }

        var destinyDb = services.GetRequiredService<IServerAdminstrationDb>();

        var result = await destinyDb.IsServerAdministratorAsync(context.Guild.Id, context.User.Id);

        return result ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is not bot admin");
    }
}
