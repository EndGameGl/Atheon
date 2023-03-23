using DotNetBungieAPI.Models.Destiny;

namespace Atheon.DataAccess.Models.Destiny;

public class DefinitionTrackSettings<TDefinition> : DefinitionTrackSettings where TDefinition : IDestinyDefinition
{
    public static DefinitionTrackSettings<TDefinition> CreateDefault()
    {
        return new DefinitionTrackSettings<TDefinition>()
        {
            TrackedHashes = new HashSet<uint>()
        };
    }
}


public abstract class DefinitionTrackSettings
{
    public HashSet<uint> TrackedHashes { get; set; }

    public bool IsTracked { get; set; }

    public bool IsReported { get; set; }

    public ulong? OverrideReportChannel { get; set; }
}