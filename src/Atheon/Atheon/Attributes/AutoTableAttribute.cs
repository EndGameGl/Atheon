namespace Atheon.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AutoTableAttribute : Attribute
{
    public string Name { get; set; }

    public AutoTableAttribute(string name)
    {
        Name = name;
    }
}
