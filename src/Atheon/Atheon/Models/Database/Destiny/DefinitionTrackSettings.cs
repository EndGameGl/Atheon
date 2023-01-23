using DotNetBungieAPI.Models.Destiny;

namespace Atheon.Models.Database.Destiny;

public class DefinitionTrackSettings<TDefinition> where TDefinition : IDestinyDefinition
{
    public HashSet<uint> TrackedHashes { get; set; }

    public bool IsTracked { get; set; }

    public bool IsReported { get; set; }

    public ulong? OverrideReportChannel { get; set; }

    public static DefinitionTrackSettings<TDefinition> CreateDefault()
    {
        return new DefinitionTrackSettings<TDefinition>()
        {
            TrackedHashes = new HashSet<uint>()
        };
    }
}
