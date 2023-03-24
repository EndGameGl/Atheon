using Atheon.DataAccess.Attributes;

namespace Atheon.DataAccess.Models.Destiny.Tracking;

[DapperAutomap]
[AutoTable("CuratedCollectibles")]
public class CuratedCollectible : TrackedDefinitionBase
{
    public static CuratedCollectible New(
        uint hash,
        string? name = null,
        string? icon = "https://www.bungie.net/img/misc/missing_icon_d2.png")
    {
        return new CuratedCollectible()
        {
            Hash = hash,
            IsEnabled = true,
            OverrideName = name,
            OverrideIcon = icon
        };
    }
}