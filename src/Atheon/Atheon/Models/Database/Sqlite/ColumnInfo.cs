using Atheon.Attributes;
using Atheon.Options;

namespace Atheon.Models.Database.Sqlite;

[DapperAutomap]
public class ColumnInfo
{
    [DapperColumn("cid")]
    public int Id { get; set; }

    [DapperColumn("name")]
    public string Name { get; set; }

    [DapperColumn("type")]
    public string Type { get; set; }

    [DapperColumn("notnull")]
    public int NotNull { get; set; }

    [DapperColumn("pk")]
    public int PrimaryKey { get; set; }

    public bool IsEqualTo(DatabaseTableColumn databaseTableColumn)
    {
        return 
            Name == databaseTableColumn.Name &&
            Type == databaseTableColumn.Type[DatabaseOptions.SqliteKey] &&
            IntToBool(NotNull) == databaseTableColumn.NotNull &&
            IntToBool(PrimaryKey) == databaseTableColumn.PrimaryKey;

    }

    private bool IntToBool(int val)
    {
        return val == 1;
    }
}
