using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Responses;

namespace Atheon.Services.Scanners.ProfileUpdaters;

public class CollectibleUpdater : IProfileUpdater
{
    private readonly ICommonEvents _commonEvents;

    public CollectibleUpdater(
        ICommonEvents commonEvents)
    {
        _commonEvents = commonEvents;
    }

    public void Update(
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse,
        List<DiscordGuildSettingsDbModel> guildSettings)
    {
        foreach (var (collectiblePointer, collectibleComponent) in profileResponse.ProfileCollectibles.Data.Collectibles)
        {
            ProcessAndAnnounceCollectible(dbProfile, collectiblePointer, collectibleComponent, guildSettings);
        }

        foreach (var (characterId, collectibleComponents) in profileResponse.CharacterCollectibles.Data)
        {
            foreach (var (collectiblePointer, collectibleComponent) in collectibleComponents.Collectibles)
            {
                ProcessAndAnnounceCollectible(dbProfile, collectiblePointer, collectibleComponent, guildSettings);
            }
        }
    }

    private void ProcessAndAnnounceCollectible(
        DestinyProfileDbModel dbProfile,
        DefinitionHashPointer<DestinyCollectibleDefinition> collectiblePointer,
        DestinyCollectibleComponent collectibleComponent,
        List<DiscordGuildSettingsDbModel> guildSettings)
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

    public void UpdateSilent(
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
