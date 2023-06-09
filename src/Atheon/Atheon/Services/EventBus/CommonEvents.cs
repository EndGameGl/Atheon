using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.Services.Interfaces;

namespace Atheon.Services.EventBus;

public class CommonEvents : ICommonEvents
{
    public IEventBus<ClanBroadcastDbModel> ClanBroadcasts { get; }

    public IEventBus<DestinyUserProfileBroadcastDbModel> ProfileBroadcasts { get; }

    public IEventBus<DestinyUserProfileCustomBroadcastDbModel> CustomProfileBroadcasts { get; }

    public CommonEvents(
        IEventBus<ClanBroadcastDbModel> clanBroadcasts,
        IEventBus<DestinyUserProfileBroadcastDbModel> profileBroadcasts,
        IEventBus<DestinyUserProfileCustomBroadcastDbModel> customProfileBroadcasts)
    {
        ClanBroadcasts = clanBroadcasts;
        ProfileBroadcasts = profileBroadcasts;
        CustomProfileBroadcasts = customProfileBroadcasts;
    }
}
