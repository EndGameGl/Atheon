namespace Atheon.DataAccess.Models.Destiny.Interfaces;

public interface IBroadcast
{
    ulong GuildId { get; set; }
    long ClanId { get; set; }
    bool WasAnnounced { get; set; }
    DateTime Date { get; set; }
}
