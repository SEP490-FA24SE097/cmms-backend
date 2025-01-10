using CMMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Constant
{
    public class CartItemRequest
    {
        public List<CartItemWithoutStoreId> CartItems { get; set; }
        public int perPage { get; set; } = 10;
        public int currentPage { get; set; } = 0;
    }

    public class UpdateCartItemRequest
    {
        public List<CartItemWithoutStoreId> CartItems { get; set; }
    }
}
