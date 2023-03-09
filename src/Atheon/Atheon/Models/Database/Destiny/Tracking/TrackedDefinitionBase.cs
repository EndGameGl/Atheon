using Atheon.Attributes;
using Atheon.Options;

namespace Atheon.Models.Database.Destiny.Tracking;

public abstract class TrackedDefinitionBase
{
    [AutoColumn(nameof(Hash), isPrimaryKey: true, notNull: true, sqliteType: DatabaseOptions.SQLiteTypes.INTEGER.BIGINT)]
    public uint Hash { get; set; }

    [AutoColumn(nameof(OverrideName), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string? OverrideName { get; set; }

    [AutoColumn(nameof(OverrideIcon), sqliteType: DatabaseOptions.SQLiteTypes.TEXT.DEFAULT_VALUE)]
    public string? OverrideIcon { get; set; }

    [AutoColumn(nameof(IsEnabled), sqliteType: DatabaseOptions.SQLiteTypes.NUMERIC.BOOLEAN)]
    public bool? IsEnabled { get; set; }
}
