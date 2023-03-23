namespace Atheon.DataAccess.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class AutoColumnAttribute : Attribute
{
    public string ColumnName { get; }

    public bool IsPrimaryKey { get; }

    public bool NotNull { get; }

    public AutoColumnAttribute(
        string name,
        bool isPrimaryKey = false,
        bool notNull = false)
    {
        ColumnName = name;
        IsPrimaryKey = isPrimaryKey;
        NotNull = notNull;
    }
}
