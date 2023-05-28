namespace Atheon.Extensions;

public static class DateTimeExtensions
{
	public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp)
	{
		var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		dateTime = dateTime.AddSeconds(unixTimeStamp);
		return dateTime;
	}
}
