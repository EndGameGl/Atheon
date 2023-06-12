using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Models;
using System.Globalization;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers.Base;

public abstract class LocalizedSlashCommandHandler : SlashCommandHandlerBase
{
    private readonly ILocalizationService _localizationService;

    protected BungieLocales GuildLocale { get; private set; }
    protected CultureInfo LocaleCulture { get; private set; }

    protected LocalizedSlashCommandHandler(
        ILocalizationService localizationService,
        ILogger logger, 
        EmbedBuilderService embedBuilderService) : base(logger, embedBuilderService)
    {
        _localizationService = localizationService;
    }

    public override async Task BeforeExecuteAsync(ICommandInfo command)
    {
        await base.BeforeExecuteAsync(command);
        GuildLocale = await _localizationService.GetGuildLocaleCachedAsync(GuildId);
        LocaleCulture = _localizationService.GetCultureForLocale(GuildLocale);
    }
}
