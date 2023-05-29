using Discord;
using Color = Discord.Color;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.PresentationNodes;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Service.Abstractions;
using System.Text;
using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Humanizer;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.DataAccess.Models.Destiny;

namespace Atheon.Services.DiscordHandlers;

public class EmbedBuilderService
{
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public EmbedBuilderService(
        DestinyDefinitionDataService destinyDefinitionDataService)
    {
        _destinyDefinitionDataService = destinyDefinitionDataService;
    }

    public EmbedBuilder GetTemplateEmbed(Color? color = null)
    {
        var embedBuilder = new EmbedBuilder();

        embedBuilder.WithCurrentTimestamp();
        embedBuilder.WithFooter("Atheon", "https://www.bungie.net/common/destiny2_content/icons/6d091410227eef82138a162df73065b9.png");
        if (!color.HasValue)
        {
            embedBuilder.WithColor(Color.Green);
        }
        else
        {
            embedBuilder.WithColor(color.Value);
        }

        return embedBuilder;
    }

    public EmbedBuilder CreateSimpleResponseEmbed(string title, string description, Color? color = null)
    {
        var embedBuilder = GetTemplateEmbed(color);

        embedBuilder.WithTitle(title);
        embedBuilder.WithDescription(description);

        return embedBuilder;
    }

    public Embed CreateErrorEmbed(Exception exception)
    {
        var embedBuilder = GetTemplateEmbed(Color.Red);

        embedBuilder.WithTitle("Failed to execute command");
        var message = $"`{exception.Message}\n{exception.Source}`\n```{exception.StackTrace}```";
        embedBuilder.WithDescription(message.Length > 4000 ? $"{message.LimitTo(3995)}\n..." : message);

        return embedBuilder.Build();
    }

    #region Clan embeds

    public Embed CreateClanBroadcastEmbed(
            ClanBroadcastDbModel clanBroadcast,
            DestinyClanDbModel destinyClanDbModel)
    {
        var templateEmbed = GetTemplateEmbed();

        templateEmbed.WithTitle($"Clan broadcast - {destinyClanDbModel.ClanName}");

        switch (clanBroadcast.Type)
        {
            case ClanBroadcastType.ClanLevel:
                AddClanLevelData(templateEmbed, clanBroadcast);
                break;
            case ClanBroadcastType.ClanName:
                AddClanNameChangeData(templateEmbed, clanBroadcast);
                break;
            case ClanBroadcastType.ClanCallsign:
                AddClanCallsignChangeData(templateEmbed, clanBroadcast);
                break;
            case ClanBroadcastType.ClanScanFinished:
                AddClanScanfinishedData(templateEmbed, destinyClanDbModel);
                break;
        }

        return templateEmbed.Build();
    }

    private static void AddClanLevelData(EmbedBuilder eb, ClanBroadcastDbModel clanBroadcast)
    {
        var message =
            $"""
                Clan reached level {clanBroadcast.NewValue}!
                """;
        eb.WithDescription(message);
    }

    private static void AddClanNameChangeData(EmbedBuilder eb, ClanBroadcastDbModel clanBroadcast)
    {
        var message =
            $"""
                Clan name changed from {clanBroadcast.OldValue} to {clanBroadcast.NewValue}
                """;
        eb.WithDescription(message);
    }

    private static void AddClanCallsignChangeData(EmbedBuilder eb, ClanBroadcastDbModel clanBroadcast)
    {
        var message =
            $"""
                Clan callsign changed from {clanBroadcast.OldValue} to {clanBroadcast.NewValue}
                """;
        eb.WithDescription(message);
    }

    private static void AddClanScanfinishedData(EmbedBuilder eb, DestinyClanDbModel destinyClanDbModel)
    {
        var message =
            $"""
                Clan scan for {destinyClanDbModel.ClanName} was finished!
                """;
        eb.WithDescription(message);
    }

    #endregion

    #region User embeds

    public Embed BuildDestinyUserBroadcast(
            DestinyUserProfileBroadcastDbModel destinyUserBroadcast,
            DestinyClanDbModel clanData,
            IBungieClient bungieClient,
            string username,
            BungieLocales locale)
    {
        var templateEmbed = GetTemplateEmbed();

        templateEmbed.WithTitle($"Clan Broadcast - {clanData.ClanName}");

        switch (destinyUserBroadcast.Type)
        {
            case ProfileBroadcastType.Collectible:
                AddCollectibleDataToEmbed(templateEmbed, destinyUserBroadcast, bungieClient, username, locale);
                break;
            case ProfileBroadcastType.Triumph:
                AddTriumphDataToEmbed(templateEmbed, destinyUserBroadcast, bungieClient, username, locale);
                break;
            case ProfileBroadcastType.Title:
                AddTitleDataToEmbed(templateEmbed, destinyUserBroadcast, bungieClient, username, locale);
                break;
            case ProfileBroadcastType.GildedTitle:
                AddTitleGildDataToEmbed(templateEmbed, destinyUserBroadcast, bungieClient, username, locale);
                break;
        }

        return templateEmbed.Build();
    }

    private void AddCollectibleDataToEmbed(
        EmbedBuilder embedBuilder,
        DestinyUserProfileBroadcastDbModel destinyUserBroadcast,
        IBungieClient bungieClient,
        string username,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(
                destinyUserBroadcast.DefinitionHash,              
                out var collectibleDefinition,
                locale))
        {
            var (name, icon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, locale);
            embedBuilder.WithThumbnailUrl(icon);

            if (destinyUserBroadcast.AdditionalData is not null &&
                destinyUserBroadcast.AdditionalData.TryGetValue("completions", out var complString))
            {
                if (int.TryParse(complString, out var activityCompletions))
                    embedBuilder.WithDescription(
                        $"{username} has obtained [{name}](https://www.light.gg/db/items/{collectibleDefinition.Item.Hash.GetValueOrDefault()}) on their {activityCompletions.Ordinalize()} clear");
                else
                    embedBuilder.WithDescription(
                        $"{username} has obtained [{name}](https://www.light.gg/db/items/{collectibleDefinition.Item.Hash.GetValueOrDefault()})");
            }
            else
            {
                embedBuilder.WithDescription(
                    $"{username} has obtained [{name}](https://www.light.gg/db/items/{collectibleDefinition.Item.Hash.GetValueOrDefault()})");
            }
        }
    }

    private static void AddTriumphDataToEmbed(
        EmbedBuilder embedBuilder,
        DestinyUserProfileBroadcastDbModel destinyUserBroadcast,
        IBungieClient bungieClient,
        string username,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                destinyUserBroadcast.DefinitionHash,
                out var recordDefinition,
                locale))
        {
            embedBuilder.WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath);

            embedBuilder.WithDescription(
                $"{username} has completed triumph: {recordDefinition.DisplayProperties.Name}");

            if (!string.IsNullOrEmpty(recordDefinition.DisplayProperties.Description))
                embedBuilder.AddField("How to complete:", recordDefinition.DisplayProperties.Description);
        }
    }

    private static void AddTitleDataToEmbed(
        EmbedBuilder embedBuilder,
        DestinyUserProfileBroadcastDbModel destinyUserBroadcast,
        IBungieClient bungieClient,
        string username,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                destinyUserBroadcast.DefinitionHash,              
                out var recordDefinition,
                locale))
        {
            if (recordDefinition.DisplayProperties.Icon.HasValue)
            {
                embedBuilder.WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath);
            }
            else
            {
                var recordTitleNode = bungieClient
                    .Repository
                    .GetAll<DestinyPresentationNodeDefinition>(locale)
                    .FirstOrDefault(x => x.CompletionRecord.Hash == destinyUserBroadcast.DefinitionHash);
                if (recordTitleNode is not null)
                    embedBuilder.WithThumbnailUrl(recordTitleNode.DisplayProperties.Icon.AbsolutePath);
            }

            embedBuilder.WithDescription(
                $"{username} has obtained title: **{recordDefinition.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine]}**");
        }
    }

    private static void AddTitleGildDataToEmbed(
        EmbedBuilder embedBuilder,
        DestinyUserProfileBroadcastDbModel destinyUserBroadcast,
        IBungieClient bungieClient,
        string username,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                destinyUserBroadcast.DefinitionHash,            
                out _,
                locale))
        {
            var titleHash = uint.Parse(destinyUserBroadcast.AdditionalData["parentTitleHash"]);
            var gildedCount = int.Parse(destinyUserBroadcast.AdditionalData["gildedCount"]);

            if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                    titleHash,                
                    out var titleRecordDefinition,
                    locale))
            {
                if (titleRecordDefinition.DisplayProperties.Icon.HasValue)
                {
                    embedBuilder.WithThumbnailUrl(titleRecordDefinition.DisplayProperties.Icon.AbsolutePath);
                }
                else
                {
                    var recordTitleNode = bungieClient
                        .Repository
                        .GetAll<DestinyPresentationNodeDefinition>(locale)
                        .FirstOrDefault(x => x.CompletionRecord.Hash == titleHash);
                    if (recordTitleNode is not null)
                        embedBuilder.WithThumbnailUrl(recordTitleNode.DisplayProperties.Icon.AbsolutePath);
                }

                embedBuilder.WithDescription(
                    $"{username} has gilded title: **{titleRecordDefinition.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine]}** {gildedCount} times!");
            }
        }
    }

    public Embed BuildDestinyUserGroupedBroadcast(
        IEnumerable<DestinyUserProfileBroadcastDbModel> destinyUserBroadcasts,
        ProfileBroadcastType broadcastType,
        uint definitionHash,
        Dictionary<long, DestinyClanDbModel> clansData,
        IBungieClient bungieClient,
        Dictionary<long, string> usernames,
        BungieLocales locale)
    {
        var embedBuilder = GetTemplateEmbed();

        embedBuilder.WithTitle("Clans Broadcast");

        switch (broadcastType)
        {
            case ProfileBroadcastType.Collectible:
                AddGroupCollectibleDataToEmbed(
                    embedBuilder,
                    destinyUserBroadcasts,
                    definitionHash,
                    clansData,
                    bungieClient,
                    usernames,
                    locale);
                break;
            case ProfileBroadcastType.Triumph:
                AddGroupTriumphDataToEmbed(
                    embedBuilder,
                    destinyUserBroadcasts,
                    definitionHash,
                    clansData,
                    bungieClient,
                    usernames,
                    locale);
                break;
            case ProfileBroadcastType.Title:
                AddGroupTitleDataToEmbed(
                    embedBuilder,
                    destinyUserBroadcasts,
                    definitionHash,
                    clansData,
                    bungieClient,
                    usernames,
                    locale);
                break;
            case ProfileBroadcastType.GildedTitle:
                var broadcast = destinyUserBroadcasts.First();
                if (broadcast.AdditionalData is not null &&
                    broadcast.AdditionalData.TryGetValue("parentTitleHash", out var parentTitleHashUnparsed) &&
                    uint.TryParse(parentTitleHashUnparsed, out var parentTitleHash))
                    AddGroupTitleGildingDataToEmbed(
                        embedBuilder,
                        destinyUserBroadcasts,
                        definitionHash,
                        parentTitleHash,
                        clansData,
                        bungieClient,
                        usernames,
                    locale);
                break;
        }

        return embedBuilder.Build();
    }

    private void AddGroupCollectibleDataToEmbed(
        EmbedBuilder embedBuilder,
        IEnumerable<DestinyUserProfileBroadcastDbModel> destinyUserBroadcasts,
        uint definitionHash,
        Dictionary<long, DestinyClanDbModel> clansData,
        IBungieClient bungieClient,
        Dictionary<long, string> usernames,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(
                definitionHash,             
                out var collectibleDefinition,
                locale))
        {
            var (name, icon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, locale);
            embedBuilder.WithDescription(
                $"{usernames.Count} people have obtained [{name}](https://www.light.gg/db/items/{collectibleDefinition.Item.Hash.GetValueOrDefault()})!");
            embedBuilder.WithThumbnailUrl(icon);

            var stringBuilder = new StringBuilder();
            foreach (var (clanId, clanData) in clansData)
            {
                stringBuilder.Clear();

                foreach (var broadcast in destinyUserBroadcasts)
                {
                    if (broadcast.ClanId != clanId)
                        continue;

                    if (usernames.TryGetValue(broadcast.MembershipId, out var username))
                    {
                        if (broadcast.AdditionalData is not null &&
                            broadcast.AdditionalData.TryGetValue("completions", out var completionsUnparsed) &&
                            int.TryParse(completionsUnparsed, out var completions))
                            stringBuilder.AppendLine($"{username} - on their {completions.Ordinalize()} clear");
                        else
                            stringBuilder.AppendLine(username);
                    }
                }

                embedBuilder.AddField($"Clan: {clanData.ClanName}", stringBuilder.ToString());
            }
        }
    }

    private static void AddGroupTriumphDataToEmbed(
        EmbedBuilder embedBuilder,
        IEnumerable<DestinyUserProfileBroadcastDbModel> destinyUserBroadcasts,
        uint definitionHash,
        Dictionary<long, DestinyClanDbModel> clansData,
        IBungieClient bungieClient,
        Dictionary<long, string> usernames,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                definitionHash,                
                out var recordDefinition,
                locale))
        {
            embedBuilder.WithDescription(
                $"{usernames.Count} people have completed triumph: **{recordDefinition.DisplayProperties.Name}**");
            embedBuilder.WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath);

            var stringBuilder = new StringBuilder();
            foreach (var (clanId, clanData) in clansData)
            {
                stringBuilder.Clear();

                foreach (var broadcast in destinyUserBroadcasts)
                {
                    if (broadcast.ClanId != clanId)
                        continue;

                    if (usernames.TryGetValue(broadcast.MembershipId, out var username))
                        stringBuilder.AppendLine(username);
                }

                embedBuilder.AddField($"Clan: {clanData.ClanName}", stringBuilder.ToString());
            }

            if (!string.IsNullOrEmpty(recordDefinition.DisplayProperties.Description))
                embedBuilder.AddField("How to complete:", recordDefinition.DisplayProperties.Description);
        }
    }

    private static void AddGroupTitleDataToEmbed(
        EmbedBuilder embedBuilder,
        IEnumerable<DestinyUserProfileBroadcastDbModel> destinyUserBroadcasts,
        uint definitionHash,
        Dictionary<long, DestinyClanDbModel> clansData,
        IBungieClient bungieClient,
        Dictionary<long, string> usernames,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                definitionHash,                
                out var recordDefinition,
                locale))
        {
            if (recordDefinition.DisplayProperties.Icon.HasValue)
            {
                embedBuilder.WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath);
            }
            else
            {
                var recordTitleNode = bungieClient
                    .Repository
                    .GetAll<DestinyPresentationNodeDefinition>(locale)
                    .FirstOrDefault(x => x.CompletionRecord.Hash == definitionHash);
                if (recordTitleNode is not null)
                    embedBuilder.WithThumbnailUrl(recordTitleNode.DisplayProperties.Icon.AbsolutePath);
            }

            embedBuilder.WithDescription(
                $"{usernames.Count} people have obtained title: **{recordDefinition.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine]}**");

            var stringBuilder = new StringBuilder();
            foreach (var (clanId, clanData) in clansData)
            {
                stringBuilder.Clear();

                foreach (var broadcast in destinyUserBroadcasts)
                {
                    if (broadcast.ClanId != clanId)
                        continue;

                    if (usernames.TryGetValue(broadcast.MembershipId, out var username))
                        stringBuilder.AppendLine(username);
                }

                embedBuilder.AddField($"Clan: {clanData.ClanName}", stringBuilder.ToString());
            }
        }
    }

    private static void AddGroupTitleGildingDataToEmbed(
        EmbedBuilder embedBuilder,
        IEnumerable<DestinyUserProfileBroadcastDbModel> destinyUserBroadcasts,
        uint definitionHash,
        uint parentTitleHash,
        Dictionary<long, DestinyClanDbModel> clansData,
        IBungieClient bungieClient,
        Dictionary<long, string> usernames,
        BungieLocales locale)
    {
        if (bungieClient.TryGetDefinition<DestinyRecordDefinition>(
                parentTitleHash,               
                out var recordDefinition,
                locale))
        {
            if (recordDefinition.DisplayProperties.Icon.HasValue)
            {
                embedBuilder.WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath);
            }
            else
            {
                var recordTitleNode = bungieClient
                    .Repository
                    .GetAll<DestinyPresentationNodeDefinition>(locale)
                    .FirstOrDefault(x => x.CompletionRecord.Hash == parentTitleHash);
                if (recordTitleNode is not null)
                    embedBuilder.WithThumbnailUrl(recordTitleNode.DisplayProperties.Icon.AbsolutePath);
            }

            embedBuilder.WithDescription(
                $"{usernames.Count} people have gilded title: **{recordDefinition.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine]}**");

            var stringBuilder = new StringBuilder();
            foreach (var (clanId, clanData) in clansData)
            {
                stringBuilder.Clear();

                foreach (var broadcast in destinyUserBroadcasts)
                {
                    if (broadcast.ClanId != clanId)
                        continue;

                    if (usernames.TryGetValue(broadcast.MembershipId, out var username) &&
                        broadcast.AdditionalData.TryGetValue("gildedCount", out var gildedCountUnparsed) &&
                        int.TryParse(gildedCountUnparsed, out var gildedCount))
                        stringBuilder.AppendLine($"{username} - {gildedCount} times");
                }

                embedBuilder.AddField($"Clan: {clanData.ClanName}", stringBuilder.ToString());
            }
        }
    }
    #endregion

    #region Leaderboards

    public string FormatAsStringTable<TEntry, TKey>(
        int supposedAmount,
        string emptySubtitle,
        List<TEntry> leaderboard,
        Func<TEntry, TKey> keyGetter,
        Func<TEntry, object>[] valueGetter,
        int limit = 4096)
    {
        if (leaderboard.Count == 0)
        {
            return emptySubtitle;
        }

        var amountIndentation = supposedAmount.ToString().Length;
        var paddingBuffer = new int[valueGetter.Length];

        for (var i = 0; i < valueGetter.Length; i++)
        {
            if (i == valueGetter.Length - 1)
            {
                paddingBuffer[i] = 0;
                continue;
            }

            var getter = valueGetter[i];
            paddingBuffer[i] = leaderboard.Select(x => getter(x).ToString().Length).Max();
        }

        var sb = new StringBuilder();

        var cutLeaderboard = leaderboard.Take(supposedAmount).ToArray();

        var valueBuffer = new object[valueGetter.Length];

        var textLength = 0;

        for (var i = 0; i < cutLeaderboard.Length; i++)
        {
            var entry = cutLeaderboard[i];

            var paddedAmount = (i + 1).ToString().PadLeft(amountIndentation);

            for (var j = 0; j < valueGetter.Length; j++)
            {
                valueBuffer[j] = valueGetter[j](entry);
            }

            var mainText = string.Join("   ", valueBuffer.Select((x, inc) => x.ToString().PadRight(paddingBuffer[inc])));

            var finalText = $"{paddedAmount}: {mainText}\n";

            if ((textLength + finalText.Length) > limit)
            {
                break;
            }

            sb.Append(finalText);
            textLength += finalText.Length;
        }

        return sb.ToString();
    }

    #endregion

    #region Custom user embeds 

    public Embed BuildDestinyCustomUserBroadcast(
        DestinyUserProfileCustomBroadcastDbModel destinyUserBroadcast,
        DestinyClanDbModel clanData,
        IBungieClient bungieClient,
        string username,
        BungieLocales locale)
    {
        var templateEmbed = GetTemplateEmbed();

        templateEmbed.WithTitle($"Clan Broadcast - {clanData.ClanName}");

        switch (destinyUserBroadcast.Type)
        {
            case ProfileCustomBroadcastType.GuardianRank:
                templateEmbed.WithDescription($"{username} guardian rank changed: {destinyUserBroadcast.OldValue} to {destinyUserBroadcast.NewValue}");
                break;
        }

        return templateEmbed.Build();
    }

    #endregion
}
