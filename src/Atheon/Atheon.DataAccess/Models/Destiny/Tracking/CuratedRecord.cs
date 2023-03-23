using Atheon.DataAccess.Attributes;

namespace Atheon.DataAccess.Models.Destiny.Tracking;

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
