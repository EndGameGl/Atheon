namespace Atheon.DataAccess.Models.Destiny.Profiles;

public class DestinyProfileLiteWithValue<TValue>
{
    public long MembershipId { get; set; }
    public string Name { get; set; }
    public long ClanId { get; set; }
    public TValue Value { get; set; }
}
