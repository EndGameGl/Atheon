using Atheon.Models.Database.Destiny;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
    public class MainProfileDataUpdater : IProfileUpdater
    {
        public bool ReliesOnSecondaryComponents => false;

        public int Priority => 1;

        public Task Update(
            IBungieClient bungieClient, 
            DestinyProfileDbModel dbProfile, 
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {
            UpdateData(dbProfile, profileResponse);
            return Task.CompletedTask;
        }

        public Task UpdateSilent(
            IBungieClient bungieClient, 
            DestinyProfileDbModel dbProfile, 
            DestinyProfileResponse profileResponse)
        {
            UpdateData(dbProfile, profileResponse);
            return Task.CompletedTask;
        }

        private void UpdateData(
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse)
        {
            var userInfo = profileResponse.Profile.Data.UserInfo;

            dbProfile.CurrentGuardianRank = profileResponse.Profile.Data.CurrentGuardianRank;
            dbProfile.LastUpdated = DateTime.UtcNow;
            dbProfile.DateLastPlayed = profileResponse.Profile.Data.DateLastPlayed;
            dbProfile.Name = $"{userInfo.BungieGlobalDisplayName}#{userInfo.BungieGlobalDisplayNameCode:D4}";
            dbProfile.MinutesPlayedTotal = profileResponse.Characters.Data.Sum(x => x.Value.MinutesPlayedTotal);
        }
    }
}
