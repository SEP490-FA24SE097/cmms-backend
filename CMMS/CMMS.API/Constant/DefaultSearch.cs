namespace CMMS.API.Constant
{
    public class DefaultSearch
    {
        public int perPage { get; set; } = 10;
        public int currentPage { get; set; } = 0;
        public String? sortBy { get; set; }
        public bool isAscending { get; set; } = true;
    }
}
