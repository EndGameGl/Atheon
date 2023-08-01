using Atheon.DataAccess;
using Atheon.DataAccess.Models.Discord;
using Atheon.DataAccess.Models.GroupSearch;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyActivities;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny.Definitions.Activities;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("fireteams", "Group of commands to create fireteams")]
public class DestinyGroupCommandHandler : LocalizedSlashCommandHandler
{
    private readonly IDestinyGroupSearchDb _destinyGroupSearchDb;
    private readonly IBungieClientProvider _bungieClientProvider;

    public DestinyGroupCommandHandler(
        ILocalizationService localizationService,
        ILogger<DestinyGroupCommandHandler> logger,
        EmbedBuilderService embedBuilderService,
        IDestinyGroupSearchDb destinyGroupSearchDb,
        IBungieClientProvider bungieClientProvider
    )
        : base(localizationService, logger, embedBuilderService)
    {
        _destinyGroupSearchDb = destinyGroupSearchDb;
        _bungieClientProvider = bungieClientProvider;
    }

    [SlashCommand("create-fireteam", "Creates new fireteam searcher")]
    public async Task CreateFireteamAsync(
        [Autocomplete(typeof(DestinyActivityDefinitionAutocompleter))]
        [Summary("activity", "Activity to look for")]
            string activityHashString,
        [Summary("time-value", "Value for time units")] int timeValue,
        [Summary("time-type", "Units for time value")] DiscordTimeType timeType
    )
    {
        DestinyGroupSearch? group = null;
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(activityHashString, out var activityHash))
                return Error(
                    FormatText(
                        "FailedToParseActivityHashError",
                        () => "Failed to parse activity hash: {0}",
                        activityHashString
                    )
                );

            var bungieClient = await _bungieClientProvider.GetClientAsync();
            if (
                !bungieClient.TryGetDefinition<DestinyActivityDefinition>(
                    activityHash,
                    out var activityDefinition,
                    GuildLocale
                )
            )
                return DestinyDefinitionNotFound<DestinyActivityDefinition>(activityHash);

            var currentTime = DateTime.UtcNow;
            group = new DestinyGroupSearch()
            {
                ActivityHash = activityHash,
                DiscordMembers = new HashSet<ulong>() { Context.User.Id },
                CreatedTime = currentTime,
                DueTo = currentTime.Add(
                    timeType switch
                    {
                        DiscordTimeType.Minutes => TimeSpan.FromMinutes(timeValue),
                        DiscordTimeType.Hours => TimeSpan.FromHours(timeValue),
                        DiscordTimeType.Days => TimeSpan.FromDays(timeValue),
                        _ => throw new NotImplementedException()
                    }
                ),
                DiscordChannelId = Context.Channel.Id,
                IsOpen = true
            };

            var maxPlayers = activityDefinition.Matchmaking.MaxParty;

            var template = EmbedBuilderService.GetTemplateEmbed(color: Color.Blue);
            template.WithTitle(
                FormatText(
                    "FireteamsTitle",
                    () => "Searching group for: {0}",
                    activityDefinition.DisplayProperties.Name
                )
            );

            var descriptionBuilder = new StringBuilder();

            var activityTypes = activityDefinition.ActivityModes
                .Select(x => x.GetValueOrNull(GuildLocale)!)
                .ToArray();
            var place = activityDefinition.Place.GetValueOrNull(GuildLocale)!;
            var destination = activityDefinition.Destination.GetValueOrNull(GuildLocale)!;

            if (destination.DisplayProperties.Name == place.DisplayProperties.Name)
            {
                descriptionBuilder.AppendLine($"- {destination.DisplayProperties.Name}");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(place.DisplayProperties.Name))
                {
                    descriptionBuilder.AppendLine($"- Place: __{place.DisplayProperties.Name}__");
                }
                if (!string.IsNullOrWhiteSpace(destination.DisplayProperties.Name))
                {
                    descriptionBuilder.AppendLine($"- Destination: __{destination.DisplayProperties.Name}__");
                }
            }
            if (activityTypes.Length > 0)
            {
                descriptionBuilder.AppendLine($"- Activity types:");
                foreach (var activityType in activityTypes)
                {
                    descriptionBuilder.AppendLine($" - {activityType.DisplayProperties.Name}");
                }
            }

            template.WithDescription(descriptionBuilder.ToString());

            var spaceLeft = maxPlayers - 1;
            template.AddField(
                $"{(spaceLeft > 0 ? $"{spaceLeft} slots left" : "No space in group")}",
                $"- {Context.User.Mention}\n- {string.Join("\n- ", Enumerable.Repeat("Empty", spaceLeft))}"
            );

            template.WithImageUrl(activityDefinition.PgcrImage.AbsolutePath);

            var cb = new ComponentBuilder();
            cb.WithButton(new ButtonBuilder().WithLabel("Join").WithStyle(ButtonStyle.Success).WithCustomId("fireteams-join-group"));
            cb.WithButton(new ButtonBuilder().WithLabel("Leave").WithStyle(ButtonStyle.Danger).WithCustomId("fireteams-leave-group"));

            return Success(template, cb);
        });

        if (group is not null)
        {
            var resp = await Context.Interaction.GetOriginalResponseAsync();
            group.DiscordMessageId = resp.Id;
            await _destinyGroupSearchDb.InsertGroupAsync(group);
        }
    }
}
