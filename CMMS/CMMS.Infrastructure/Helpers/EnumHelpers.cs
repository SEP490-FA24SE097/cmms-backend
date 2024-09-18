namespace CMMS.API.Helpers
{

        public static class EnumHelpers
        {
            public static List<string> GetEnumValues<T>() where T : Enum
            {
                // Get all values of the enum and convert them to a list of strings
                return Enum.GetValues(typeof(T)).Cast<T>().Select(e => e.ToString()).ToList();
            }
        }
}
