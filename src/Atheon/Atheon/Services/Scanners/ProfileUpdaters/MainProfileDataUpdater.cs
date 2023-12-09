using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.Destiny2.Metadata;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters
{
    public class MainProfileDataUpdater : IProfileUpdater
    {
        private readonly ICommonEvents _commonEvents;

        public bool ReliesOnSecondaryComponents => false;

        public int Priority => 1;

        public MainProfileDataUpdater(ICommonEvents commonEvents)
        {
            _commonEvents = commonEvents;
        }

        public Task Update(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {
            UpdateData(dbProfile, profileResponse, guildSettings);
            return Task.CompletedTask;
        }

        public Task UpdateSilent(
            IBungieClient bungieClient,
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse)
        {
            UpdateDataSilent(dbProfile, profileResponse);
            return Task.CompletedTask;
        }

        private void UpdateData(
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {
            var userInfo = profileResponse.Profile.Data.UserInfo;

            UpdateGuardianRank(dbProfile, profileResponse, guildSettings);
            dbProfile.LastUpdated = DateTime.UtcNow;
            dbProfile.DateLastPlayed = profileResponse.Profile.Data.DateLastPlayed;
            dbProfile.Name = $"{userInfo.BungieGlobalDisplayName}#{userInfo.BungieGlobalDisplayNameCode:D4}";
            dbProfile.MinutesPlayedTotal = profileResponse.Characters.Data.Sum(x => x.Value.MinutesPlayedTotal);
            dbProfile.GameVersionsOwned = profileResponse.Profile.Data.VersionsOwned;

            var lastPlayedCharacterId = profileResponse.GetLastPlayedCharacterId();
            if (lastPlayedCharacterId.HasValue)
            {
                dbProfile.CurrentActivityData ??= new DataAccess.Models.Destiny.Profiles.PlayerActivityData();
                var charId = lastPlayedCharacterId.Value;
                var charActivities = profileResponse.CharacterActivities.Data[charId];
                dbProfile.CurrentActivityData.ActivityHash = charActivities.CurrentActivity.Hash;
                dbProfile.CurrentActivityData.PlaylistActivityHash = charActivities.CurrentPlaylistActivity.Hash;
                dbProfile.CurrentActivityData.DateActivityStarted = charActivities.DateActivityStarted;
                dbProfile.CurrentActivityData.ActivityModeHash = charActivities.CurrentActivityMode.Hash;
                dbProfile.CurrentActivityData.ActivityModeHashes = [];
                foreach (var mode in charActivities.CurrentActivityModes)
                {
                    var hash = mode.Hash.GetValueOrDefault();
                    dbProfile.CurrentActivityData.ActivityModeHashes.Add(hash);
                }
            }
        }

        private void UpdateDataSilent(
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse)
        {
            var userInfo = profileResponse.Profile.Data.UserInfo;

            dbProfile.CurrentGuardianRank = profileResponse.Profile.Data.CurrentGuardianRank;
            dbProfile.LastUpdated = DateTime.UtcNow;
            dbProfile.DateLastPlayed = profileResponse.Profile.Data.DateLastPlayed;
            dbProfile.Name = $"{userInfo.BungieGlobalDisplayName}#{userInfo.BungieGlobalDisplayNameCode:D4}";
            dbProfile.MinutesPlayedTotal = profileResponse.Characters.Data.Sum(x => x.Value.MinutesPlayedTotal);
            dbProfile.GameVersionsOwned = profileResponse.Profile.Data.VersionsOwned;

            var lastPlayedCharacterId = profileResponse.GetLastPlayedCharacterId();
            if (lastPlayedCharacterId.HasValue)
            {
                dbProfile.CurrentActivityData ??= new DataAccess.Models.Destiny.Profiles.PlayerActivityData();
                var charId = lastPlayedCharacterId.Value;
                var charActivities = profileResponse.CharacterActivities.Data[charId];
                dbProfile.CurrentActivityData.ActivityHash = charActivities.CurrentActivity.Hash;
                dbProfile.CurrentActivityData.PlaylistActivityHash = charActivities.CurrentPlaylistActivity.Hash;
                dbProfile.CurrentActivityData.DateActivityStarted = charActivities.DateActivityStarted;
                dbProfile.CurrentActivityData.ActivityModeHash = charActivities.CurrentActivityMode.Hash;
                dbProfile.CurrentActivityData.ActivityModeHashes = [];
                foreach (var mode in charActivities.CurrentActivityModes)
                {
                    var hash = mode.Hash.GetValueOrDefault();
                    dbProfile.CurrentActivityData.ActivityModeHashes.Add(hash);
                }
            }
        }

        private void UpdateGuardianRank(
            DestinyProfileDbModel dbProfile,
            DestinyProfileResponse profileResponse,
            List<DiscordGuildSettingsDbModel> guildSettings)
        {
            if (dbProfile.CurrentGuardianRank != profileResponse.Profile.Data.CurrentGuardianRank)
            {
                foreach (var guildSetting in guildSettings)
                {
                    if (!guildSetting.ReportClanChanges)
                    {
                        continue;
                    }

                    _commonEvents.CustomProfileBroadcasts.Publish(new DestinyUserProfileCustomBroadcastDbModel()
                    {
                        Type = ProfileCustomBroadcastType.GuardianRank,
                        ClanId = dbProfile.ClanId.GetValueOrDefault(),
                        Date = DateTime.UtcNow,
                        GuildId = guildSetting.GuildId,
                        OldValue = dbProfile.CurrentGuardianRank.ToString(),
                        NewValue = profileResponse.Profile.Data.CurrentGuardianRank.ToString(),
                        MembershipId = dbProfile.MembershipId,
                        WasAnnounced = false
                    });
                }
            }
            dbProfile.CurrentGuardianRank = profileResponse.Profile.Data.CurrentGuardianRank;
        }
    }
}
