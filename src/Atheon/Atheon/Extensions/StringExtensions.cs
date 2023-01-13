namespace Atheon.Extensions;

public static class StringExtensions
{
    public static bool IsNotNullOrEmpty(this string? value)
    {
        return !string.IsNullOrEmpty(value);
    }

    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }
}
