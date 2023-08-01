using Atheon.DataAccess;
using Atheon.Services.Interfaces;

namespace Atheon.Services.BungieApi
{
    public class BroadcastSaver
    {
        private readonly ICommonEvents _commonEvents;
        private readonly IBroadcastDb _broadcastDb;

        public BroadcastSaver(
            ICommonEvents commonEvents,
            IBroadcastDb broadcastDb)
        {
            _commonEvents = commonEvents;
            _broadcastDb = broadcastDb;
            _commonEvents.ClanBroadcasts.Published += (e) =>
            {
                _ = _broadcastDb.TryInsertClanBroadcastAsync(e);
            };
            _commonEvents.ProfileBroadcasts.Published += (e) =>
            {
                _ = _broadcastDb.TryInsertProfileBroadcastAsync(e);
            };
            _commonEvents.CustomProfileBroadcasts.Published += (e) =>
            {
                _ = _broadcastDb.TryInsertProfileCustomBroadcastAsync(e);
            };
        }
    }
}
