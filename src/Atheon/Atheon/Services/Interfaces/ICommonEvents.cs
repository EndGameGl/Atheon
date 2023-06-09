using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.Services.EventBus;

namespace Atheon.Services.Interfaces;

public interface ICommonEvents
{
    IEventBus<ClanBroadcastDbModel> ClanBroadcasts { get; }
    IEventBus<DestinyUserProfileBroadcastDbModel> ProfileBroadcasts { get; }
    IEventBus<DestinyUserProfileCustomBroadcastDbModel> CustomProfileBroadcasts { get; }
}
