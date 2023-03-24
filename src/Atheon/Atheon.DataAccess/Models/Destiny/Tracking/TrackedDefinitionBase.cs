using Atheon.DataAccess.Attributes;

namespace Atheon.DataAccess.Models.Destiny.Tracking;

public abstract class TrackedDefinitionBase
{
    [AutoColumn(nameof(Hash), isPrimaryKey: true, notNull: true)]
    public uint Hash { get; set; }

    [AutoColumn(nameof(OverrideName))]
    public string? OverrideName { get; set; }

    [AutoColumn(nameof(OverrideIcon))]
    public string? OverrideIcon { get; set; }

    [AutoColumn(nameof(IsEnabled))]
    public bool? IsEnabled { get; set; }
}
