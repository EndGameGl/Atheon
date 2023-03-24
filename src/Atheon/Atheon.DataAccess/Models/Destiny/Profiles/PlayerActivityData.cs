namespace Atheon.DataAccess.Models.Destiny.Profiles;

public class PlayerActivityData
{
    public uint? ActivityHash { get; set; }

    public uint? ActivityModeHash { get; set; }

    public List<uint>? ActivityModeHashes { get; set; }

    public uint? PlaylistActivityHash { get; set; }

    public DateTime? DateActivityStarted { get; set; }
}
