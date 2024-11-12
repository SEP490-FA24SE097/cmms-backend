using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class GoodsDeliveryNoteCM
    {
        public string? StoreId { get; set; }
        public string ReasonDescription { get; set; }
        public Decimal Total { get; set; }
        public string TotalByText { get; set; }
        public List<GoodsDeliveryNoteDetailCM> Details { get; set; }
    }

    public class GoodsDeliveryNoteDetailCM
    {
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public Decimal Quantity { get; set; }
        public Decimal UnitPrice { get; set; }
        public Decimal Total { get; set; }
    }
}
