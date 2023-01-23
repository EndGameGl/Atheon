using DotNetBungieAPI.Models.GroupsV2;
using DotNetBungieAPI.Service.Abstractions;
using System.Collections.ObjectModel;

namespace Atheon.Services.Scanners.DestinyClanScanner
{
    public class DestinyClanScannerContext
    {
        public IBungieClient? BungieClient { get; set; }

        public GroupResponse? ClanData { get; set; }

        public ReadOnlyCollection<GroupMember> Members { get; set; }

        public long ClanId { get; set; }
        public int MembersOnline { get; set; }
        public List<GroupMember> MembersToScan { get; set; }
    }
}
