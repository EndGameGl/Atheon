using Atheon.DataAccess;
using Atheon.Services.Interfaces;

namespace Atheon.Services.BungieApi
{
    public class BroadcastSaver
    {
        private readonly ICommonEvents _commonEvents;
        private readonly IDestinyDb _destinyDb;

        public BroadcastSaver(
            ICommonEvents commonEvents,
            IDestinyDb destinyDb)
        {
            _commonEvents = commonEvents;
            _destinyDb = destinyDb;
            _commonEvents.ClanBroadcasts.Published += (e) =>
            {
                _ = _destinyDb.TryInsertClanBroadcastAsync(e);
            };
            _commonEvents.ProfileBroadcasts.Published += (e) =>
            {
                _ = _destinyDb.TryInsertProfileBroadcastAsync(e);
            };
            _commonEvents.CustomProfileBroadcasts.Published += (e) =>
            {
                _ = _destinyDb.TryInsertProfileCustomBroadcastAsync(e);
            };
        }
    }
}
