namespace Atheon.DataAccess.Models.Destiny.Profiles;

public class DestinyProfileLiteWithDoubleValues<TFirstValue, TSecondValue>
{
    public long MembershipId { get; set; }
    public string Name { get; set; }
    public long ClanId { get; set; }
    public TFirstValue FirstValue { get; set; }
    public TSecondValue SecondValue { get; set; }
}
