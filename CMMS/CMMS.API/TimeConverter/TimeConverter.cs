namespace CMMS.API.TimeConverter
{
    public static class TimeConverter
    {
        public static DateTime GetVietNamTime()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }
    }
}
