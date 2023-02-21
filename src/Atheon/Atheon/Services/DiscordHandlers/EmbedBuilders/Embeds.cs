using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Discord;

namespace Atheon.Services.DiscordHandlers.EmbedBuilders;

public static partial class Embeds
{
    public static EmbedBuilder GetTemplateEmbed()
    {
        var embedBuilder = new EmbedBuilder();

        embedBuilder.WithCurrentTimestamp();
        embedBuilder.WithFooter("Atheon", "https://www.bungie.net/common/destiny2_content/icons/6d091410227eef82138a162df73065b9.png");

        return embedBuilder;
    }

    public static EmbedBuilder GetGenericEmbed(string title, Color color, string description)
    {
        var embedBuilder = new EmbedBuilder();

        embedBuilder.WithTitle(title);
        embedBuilder.WithCurrentTimestamp();
        embedBuilder.WithColor(color);
        embedBuilder.WithDescription(description);
        embedBuilder.WithFooter("Atheon", "https://www.bungie.net/common/destiny2_content/icons/6d091410227eef82138a162df73065b9.png");

        return embedBuilder;
    }

    public static class Broadcasts
    {
        public static Embed Clan(
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
                    AddClanScanfinishedData(templateEmbed, clanBroadcast, destinyClanDbModel);
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

        private static void AddClanScanfinishedData(EmbedBuilder eb, ClanBroadcastDbModel clanBroadcast, DestinyClanDbModel destinyClanDbModel)
        {
            var message =
                $"""
                Clan scan for {destinyClanDbModel.ClanName} was finished!
                """;
            eb.WithDescription(message);
        }

        public static Embed Profile(
            DestinyUserProfileBroadcastDbModel userBroadcast)
        {

        }
    }
}
