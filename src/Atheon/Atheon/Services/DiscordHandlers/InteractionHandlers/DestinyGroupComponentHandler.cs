using Atheon.DataAccess;
using Atheon.DataAccess.Models.GroupSearch;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny.Definitions.Activities;
using DotNetBungieAPI.Service.Abstractions;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

public class DestinyGroupComponentHandler : LocalizedComponentCommandHandlerBase
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly IDestinyGroupSearchDb _destinyGroupSearchDb;

    public DestinyGroupComponentHandler(
        IBungieClientProvider bungieClientProvider,
        IDestinyGroupSearchDb destinyGroupSearchDb,
        ILocalizationService localizationService,
        ILogger<DestinyGroupComponentHandler> logger,
        EmbedBuilderService embedBuilderService) : base(localizationService, logger, embedBuilderService)
    {
        _bungieClientProvider = bungieClientProvider;
        _destinyGroupSearchDb = destinyGroupSearchDb;
    }


    [ComponentInteraction("fireteams-join-group")]
    public async Task JoinGroupAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var messageId = Context.Interaction.Message.Id;
            var channelId = Context.Interaction.Channel.Id;
            var client = await _bungieClientProvider.GetClientAsync();
            var group = await _destinyGroupSearchDb.GetGroupAsync(channelId, messageId);
            var activityHash = (uint)group!.ActivityHash;
            if (!client.Repository.TryGetDestinyDefinition<DestinyActivityDefinition>(activityHash, out var activityDefinition, GuildLocale))
            {
                return this.DestinyDefinitionNotFound<DestinyActivityDefinition>(activityHash);
            }

            var maxPlayers = activityDefinition.Matchmaking.MaxParty;
            var spaceLeft = maxPlayers - group.DiscordMembers.Count;
            if (spaceLeft <= 0)
            {
                return Success(EmbedBuilderService.GetTemplateEmbed().WithDescription("No space left"), hide: true);
            }

            if (group.DiscordMembers.Contains(Context.User.Id))
            {
                return Edit(GetEmbedForGroup(group, activityDefinition, maxPlayers - group.DiscordMembers.Count), GetComponentsForGroup(group.DiscordMembers.Count, maxPlayers));
            }

            group.DiscordMembers.Add(Context.User.Id);
            await _destinyGroupSearchDb.UpdateGroupMembersAsync(group);

            return Edit(GetEmbedForGroup(group, activityDefinition, maxPlayers - group.DiscordMembers.Count), GetComponentsForGroup(group.DiscordMembers.Count, maxPlayers));
        });
    }

    [ComponentInteraction("fireteams-leave-group")]
    public async Task LeaveGroupAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var messageId = Context.Interaction.Message.Id;
            var channelId = Context.Interaction.Channel.Id;
            var client = await _bungieClientProvider.GetClientAsync();
            var group = await _destinyGroupSearchDb.GetGroupAsync(channelId, messageId);
            var activityHash = (uint)group!.ActivityHash;
            if (!client.Repository.TryGetDestinyDefinition<DestinyActivityDefinition>(activityHash, out var activityDefinition, GuildLocale))
            {
                return this.DestinyDefinitionNotFound<DestinyActivityDefinition>(activityHash);
            }

            var maxPlayers = activityDefinition.Matchmaking.MaxParty;
            var spaceLeft = maxPlayers - group.DiscordMembers.Count;

            if (!group.DiscordMembers.Contains(Context.User.Id))
            {
                return Edit(GetEmbedForGroup(group, activityDefinition, maxPlayers - group.DiscordMembers.Count), GetComponentsForGroup(group.DiscordMembers.Count, maxPlayers));
            }

            group.DiscordMembers.Remove(Context.User.Id);
            await _destinyGroupSearchDb.UpdateGroupMembersAsync(group);

            return Edit(GetEmbedForGroup(group, activityDefinition, maxPlayers - group.DiscordMembers.Count), GetComponentsForGroup(group.DiscordMembers.Count, maxPlayers));
        });
    }

    private EmbedBuilder GetEmbedForGroup(DestinyGroupSearch group, DestinyActivityDefinition activityDefinition, int spaceLeft)
    {
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

        var users = group.DiscordMembers.Select(x => Context.Client.GetUser(x).Mention).ToArray();

        var membersStringBuilder = new StringBuilder();
        if (users.Any())
        {
            membersStringBuilder.Append($"- {string.Join("\n- ", users)}\n");
        }

        if (spaceLeft > 0)
        {
            membersStringBuilder.Append($"- {string.Join("\n- ", Enumerable.Repeat("Empty", spaceLeft))}");
        }

        template.AddField(
            $"{(spaceLeft > 0 ? $"{spaceLeft} slots left" : "No space in group")}",
            membersStringBuilder.ToString()
        );

        template.WithImageUrl(activityDefinition.PgcrImage.AbsolutePath);

        return template;
    }

    private ComponentBuilder GetComponentsForGroup(int current, int max)
    {
        var cb = new ComponentBuilder();
        cb.WithButton(new ButtonBuilder().WithLabel("Join").WithStyle(ButtonStyle.Success).WithCustomId("fireteams-join-group").WithDisabled(current == max));
        cb.WithButton(new ButtonBuilder().WithLabel("Leave").WithStyle(ButtonStyle.Danger).WithCustomId("fireteams-leave-group").WithDisabled(current == 0));
        return cb;
    }
}
