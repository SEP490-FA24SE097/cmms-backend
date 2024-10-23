using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Identity.Client;

namespace CMMS.Core.Entities
{
    public class GoodsDeliveryNote
    {
        [Key]
        public Guid Id { get; set; }
        public string ReasonDescription { get; set; }
        public Decimal Total { get; set; }
        public string TotalByText { get; set; }
        public DateTime TimeStamp { get; set; }
        public virtual ICollection<GoodsDeliveryNoteDetail> GoodsDeliveryNoteDetails { get; set; }
    }
}
