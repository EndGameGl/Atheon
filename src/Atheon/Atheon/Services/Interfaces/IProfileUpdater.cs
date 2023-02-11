using Atheon.Models.Database.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Interfaces
{
    public interface IProfileUpdater
    {
        bool ReliesOnSecondaryComponents { get; }

        void UpdateSilent(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse);

        void Update(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings);
    }
}
