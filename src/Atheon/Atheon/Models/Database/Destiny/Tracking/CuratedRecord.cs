using Atheon.Attributes;

namespace Atheon.Models.Database.Destiny.Tracking;

[DapperAutomap]
[AutoTable("CuratedRecords")]
public class CuratedRecord : TrackedDefinitionBase
{

    public static CuratedRecord New(uint hash)
    {
        return new CuratedRecord()
        {
            Hash = hash,
            IsEnabled = true
        };
    }
}
