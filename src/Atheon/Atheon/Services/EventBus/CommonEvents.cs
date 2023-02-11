using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.Interfaces;

namespace Atheon.Services.EventBus;

public class CommonEvents : ICommonEvents
{
    public IEventBus<ClanBroadcastDbModel> ClanBroadcasts { get; }

    public IEventBus<DestinyUserProfileBroadcastDbModel> ProfileBroadcasts { get; }

    public CommonEvents(
        IEventBus<ClanBroadcastDbModel> clanBroadcasts,
        IEventBus<DestinyUserProfileBroadcastDbModel> profileBroadcasts)
    {
        ClanBroadcasts = clanBroadcasts;
        ProfileBroadcasts = profileBroadcasts;
    }
}
