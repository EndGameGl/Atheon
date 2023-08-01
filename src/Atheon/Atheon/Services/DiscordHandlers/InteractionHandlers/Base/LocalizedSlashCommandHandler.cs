using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Models;
using System.Globalization;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers.Base;

public abstract class LocalizedSlashCommandHandler : SlashCommandHandlerBase
{
    private readonly ILocalizationService _localizationService;

    protected BungieLocales GuildLocale { get; private set; }
    protected CultureInfo LocaleCulture { get; private set; } = null!;

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

    public string FormatText(string id, Func<string> defaultText, params object[] parameters)
    {
        var text = _localizationService.GetLocalizedText(id, GuildLocale, defaultText);
        return string.Format(text, parameters);
    }

    public string Text(string id, Func<string> defaultText)
    {
        return _localizationService.GetLocalizedText(id, GuildLocale, defaultText);
    }
}
