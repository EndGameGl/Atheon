namespace Atheon.Attributes;

public class DapperColumnAttribute : Attribute
{
    public string ColumnName { get; }
    public DapperColumnAttribute(string name)
    {
        ColumnName = name;
    }
}
