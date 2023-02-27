using Atheon.Attributes;

namespace Atheon.Models.Database.Destiny.Tracking;

[DapperAutomap]
[AutoTable("CuratedCollectibles")]
public class CuratedCollectible : TrackedDefinitionBase
{
    public static CuratedCollectible New(uint hash)
    {
        return new CuratedCollectible()
        {
            Hash = hash,
            IsEnabled = true
        };
    }
}
