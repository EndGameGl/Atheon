namespace Atheon.Options;

public class DatabaseOptions
{
    public const string SqliteKey = "Sqlite";

    public Dictionary<string, DatabaseOptionsEntry> Databases { get;set; }
    public Dictionary<string, DatabaseTableEntry> Tables { get; set; }
}
