using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Seasons;
using Humanizer;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("destiny-time-info", "Group of commands to show time-related info for Destiny 2")]
public class DestinyResetInfoCommandHandler : LocalizedSlashCommandHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;

    public DestinyResetInfoCommandHandler(
        ILogger<DestinyResetInfoCommandHandler> logger,
        EmbedBuilderService embedBuilderService,
        IBungieClientProvider bungieClientProvider,
        ILocalizationService localizationService) : base(localizationService, logger, embedBuilderService)
    {
        _bungieClientProvider = bungieClientProvider;
    }

    [SlashCommand("daily-reset", "Shows how much time is left until daily reset")]
    public async Task GetTimeUntilDailyResetAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var bungieClient = await _bungieClientProvider.GetClientAsync();
            var resetTime = bungieClient.ResetService.GetNextDailyReset();
            var currentTime = DateTime.UtcNow;
            var timeLeft = currentTime - resetTime;
            var humanizedTime = timeLeft.Humanize(culture: LocaleCulture, minUnit: Humanizer.Localisation.TimeUnit.Minute, precision: 2);
            var embed = EmbedBuilderService.CreateSimpleResponseEmbed(
                Text("TimeUntilDailyReset", () => "Time until daily reset"),
                FormatText("TimeUntilDailyResetDescription", () => "Daily reset will happen in {0}", humanizedTime));
            return Success(embed);
        });
    }

    [SlashCommand("weekly-reset", "Shows how much time is left until weekly reset")]
    public async Task GetTimeUntilWeeklyResetAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var bungieClient = await _bungieClientProvider.GetClientAsync();
            var resetTime = bungieClient.ResetService.GetNextWeeklyReset(DayOfWeek.Tuesday);
            var currentTime = DateTime.UtcNow;
            var timeLeft = currentTime - resetTime;
            var humanizedTime = timeLeft.Humanize(culture: LocaleCulture, minUnit: Humanizer.Localisation.TimeUnit.Minute, precision: 2);
            var embed = EmbedBuilderService.CreateSimpleResponseEmbed(
                Text("TimeUntilWeeklyReset", () => "Time until weekly reset"),
                 FormatText("TimeUntilWeeklyResetDescription", () => "Weekly reset will happen in {0}", humanizedTime));
            return Success(embed);
        });
    }

    [SlashCommand("season-end", "Shows how much time is left until season ends")]
    public async Task GetTimeUntilSeasonEndsAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var bungieClient = await _bungieClientProvider.GetClientAsync();
            var currentTime = DateTime.UtcNow;
            var currentSeason = bungieClient.Repository
                .Search<DestinySeasonDefinition>(x => x.EndDate is not null && (currentTime < x.EndDate && currentTime >= x.StartDate), GuildLocale)
                .FirstOrDefault();

            if (currentSeason is null)
            {
                return Error(Text("FailedToResolveCurrentSeasonError", () => "Current season couldn't be determined"));
            }

            var timeLeft = currentSeason.EndDate!.Value - currentTime;
            var humanizedTime = timeLeft.Humanize(culture: LocaleCulture, minUnit: Humanizer.Localisation.TimeUnit.Minute, precision: 2);
            var embed = EmbedBuilderService.CreateSimpleResponseEmbed(
                Text("TimeUntilSeasonEnds", () => "Time until season ends"),
                FormatText("TimeUntilSeasonEndsDescription", () => "{0} will end in {1}", currentSeason.DisplayProperties.Name, humanizedTime));
            return Success(embed);
        });
    }
}
