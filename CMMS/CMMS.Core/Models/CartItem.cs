using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class CartItem
    {
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string StoreId { get; set; }
        public decimal Quantity { get; set; }
    }
}
