namespace Atheon.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class AutoColumnAttribute : Attribute
{
    public string ColumnName { get; }

    public bool IsPrimaryKey { get; }
    
    public bool NotNull { get; }

    public string? SqliteType { get; }

    public AutoColumnAttribute(
        string name, 
        bool isPrimaryKey = false,
        bool notNull = false,
        string? sqliteType = null)
    {
        ColumnName = name;
        IsPrimaryKey = isPrimaryKey;
        NotNull = notNull;
        SqliteType = sqliteType;
    }
}
