using DotNetBungieAPI.Models.Destiny.Responses;

namespace Atheon.Extensions;

public static class DestinyExtensions
{
    public static bool HasPublicRecords(this DestinyProfileResponse profileResponse)
    {
        return profileResponse.ProfileRecords.Data is not null;
    }
}
