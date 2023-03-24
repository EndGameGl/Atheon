using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.Destiny2.Metadata;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters;

public class CollectibleUpdater : IProfileUpdater
{
    private readonly ICommonEvents _commonEvents;

    public bool ReliesOnSecondaryComponents => true;
    public int Priority => 0;

    public CollectibleUpdater(
        ICommonEvents commonEvents)
    {
        _commonEvents = commonEvents;
    }

    public async Task Update(
        IBungieClient bungieClient,
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse,
        List<DiscordGuildSettingsDbModel> guildSettings)
    {
        foreach (var (collectiblePointer, collectibleComponent) in profileResponse.ProfileCollectibles.Data.Collectibles)
        {
            ProcessAndAnnounceCollectible(dbProfile, collectiblePointer, collectibleComponent, guildSettings, profileResponse);
        }

        foreach (var (characterId, collectibleComponents) in profileResponse.CharacterCollectibles.Data)
        {
            foreach (var (collectiblePointer, collectibleComponent) in collectibleComponents.Collectibles)
            {
                ProcessAndAnnounceCollectible(dbProfile, collectiblePointer, collectibleComponent, guildSettings, profileResponse);
            }
        }
    }

    private void ProcessAndAnnounceCollectible(
        DestinyProfileDbModel dbProfile,
        DefinitionHashPointer<DestinyCollectibleDefinition> collectiblePointer,
        DestinyCollectibleComponent collectibleComponent,
        List<DiscordGuildSettingsDbModel> guildSettings,
        DestinyProfileResponse profileResponse)
    {
        if (collectibleComponent.State.HasFlag(DotNetBungieAPI.Models.Destiny.DestinyCollectibleState.NotAcquired))
            return;

        var collectibleHashValue = collectiblePointer.Hash.GetValueOrDefault();

        if (!dbProfile.Collectibles.Add(collectibleHashValue))
            return;

        foreach (var guildSetting in guildSettings)
        {
            if (!guildSetting.TrackedCollectibles.IsReported)
                continue;

            if (!guildSetting.TrackedCollectibles.TrackedHashes.Contains(collectibleHashValue))
                continue;

            if (Destiny2Metadata.DryStreakItemSettings.TryGetValue(collectibleHashValue, out var metricHash) &&
                profileResponse.Metrics.Data.Metrics.TryGetValue(metricHash, out var metricComponent))
            {
                _commonEvents.ProfileBroadcasts.Publish(new DestinyUserProfileBroadcastDbModel()
                {
                    Date = DateTime.UtcNow,
                    ClanId = dbProfile.ClanId.GetValueOrDefault(),
                    WasAnnounced = false,
                    DefinitionHash = collectibleHashValue,
                    GuildId = guildSetting.GuildId,
                    MembershipId = dbProfile.MembershipId,
                    Type = ProfileBroadcastType.Collectible,
                    AdditionalData = new Dictionary<string, string>()
                    {
                        { "completions", (metricComponent.ObjectiveProgress.Progress ?? 0).ToString() }
                    }
                });
            }
            else
            {
                _commonEvents.ProfileBroadcasts.Publish(new DestinyUserProfileBroadcastDbModel()
                {
                    Date = DateTime.UtcNow,
                    ClanId = dbProfile.ClanId.GetValueOrDefault(),
                    WasAnnounced = false,
                    DefinitionHash = collectibleHashValue,
                    GuildId = guildSetting.GuildId,
                    MembershipId = dbProfile.MembershipId,
                    Type = ProfileBroadcastType.Collectible,
                    AdditionalData = null
                });
            }
        }
    }

    public async Task UpdateSilent(
        IBungieClient bungieClient,
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse)
    {
        foreach (var (collectiblePointer, collectibleComponent) in profileResponse.ProfileCollectibles.Data.Collectibles)
        {
            ProcessCollectible(dbProfile, collectiblePointer, collectibleComponent);
        }

        foreach (var (characterId, collectibleComponents) in profileResponse.CharacterCollectibles.Data)
        {
            foreach (var (collectiblePointer, collectibleComponent) in collectibleComponents.Collectibles)
            {
                ProcessCollectible(dbProfile, collectiblePointer, collectibleComponent);
            }
        }
    }

    private void ProcessCollectible(
        DestinyProfileDbModel dbProfile,
        DefinitionHashPointer<DestinyCollectibleDefinition> collectiblePointer,
        DestinyCollectibleComponent collectibleComponent)
    {
        if (collectibleComponent.State.HasFlag(DotNetBungieAPI.Models.Destiny.DestinyCollectibleState.NotAcquired))
            return;

        var collectibleHashValue = collectiblePointer.Hash.GetValueOrDefault();

        if (!dbProfile.Collectibles.Add(collectibleHashValue))
            return;
    }
}
