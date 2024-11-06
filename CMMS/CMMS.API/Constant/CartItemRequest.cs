using CMMS.Core.Models;

namespace CMMS.API.Constant
{
    public class CartItemRequest
    {
        public List<CartItem> CartItems { get; set; }
        public int perPage { get; set; } = 10;
        public int currentPage { get; set; } = 0;
    }
}
