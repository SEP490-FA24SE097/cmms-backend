using System.Runtime.InteropServices;

namespace CMMS.API.TimeConverter
{
    public static class TimeConverter
    {
        public static DateTime GetVietNamTime()
        {
            var vietnamOffset = TimeSpan.FromHours(7);

            
            var utcNow = DateTimeOffset.UtcNow;

            return utcNow.ToOffset(vietnamOffset).DateTime;

        }
    }
}
