namespace Atheon.Options;

public class DatabaseTableColumn
{
    public string Name { get; set; }

    /// <summary>
    /// Types keyed by db type
    /// </summary>
    public Dictionary<string, string> Type { get; set; }

    public bool? PrimaryKey { get; set; }

    public bool? NotNull { get; set; }

    public string FormatForCreateQuery(string dbType)
    {
        switch (dbType)
        {
            case DatabaseOptions.SqliteKey:
                return $"{Name} {Type[dbType]}{(PrimaryKey.HasValue ? " PRIMARY KEY" : string.Empty)}{(NotNull.HasValue ? " NOT NULL" : string.Empty)}";
            default:
                return string.Empty;
        }
    }
}
