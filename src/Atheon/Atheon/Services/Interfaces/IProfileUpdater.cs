using Atheon.Models.Database.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;

namespace Atheon.Services.Interfaces
{
    public interface IProfileUpdater
    {
        void UpdateSilent(DestinyProfileDbModel dbProfile, DestinyProfileResponse profileResponse);
        void Update(DestinyProfileDbModel dbProfile, DestinyProfileResponse profileResponse, List<DiscordGuildSettingsDbModel> guildSettings);
    }
}
