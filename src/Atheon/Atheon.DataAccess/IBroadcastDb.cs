using Atheon.DataAccess.Models.Destiny.Broadcasts;

namespace Atheon.DataAccess;

public interface IBroadcastDb
{
    Task TryInsertClanBroadcastAsync(ClanBroadcastDbModel clanBroadcast);
    Task TryInsertProfileBroadcastAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);
    Task TryInsertProfileCustomBroadcastAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast);

    Task MarkClanBroadcastSentAsync(ClanBroadcastDbModel clanBroadcast);
    Task MarkUserBroadcastSentAsync(DestinyUserProfileBroadcastDbModel profileBroadcast);
    Task MarkUserCustomBroadcastSentAsync(DestinyUserProfileCustomBroadcastDbModel profileCustomBroadcast);
}
