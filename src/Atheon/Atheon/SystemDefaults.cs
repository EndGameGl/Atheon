using System.Globalization;

namespace Atheon;

public static class SystemDefaults
{
    public static CultureInfo DefaultCulture { get; } = CultureInfo.GetCultureInfo("en-US");
}
