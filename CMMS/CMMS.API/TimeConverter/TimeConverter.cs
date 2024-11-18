using System.Runtime.InteropServices;

namespace CMMS.API.TimeConverter
{
    public static class TimeConverter
    {
        public static DateTime GetVietNamTime()
        {
            var timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time" // Windows ID
                : "Asia/Bangkok";         // IANA ID for other platforms
            return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
    }
}
