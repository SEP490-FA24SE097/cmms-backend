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
    public class GoodsNote
    {
        [Key]
        public Guid Id { get; set; }
        public string? StoreId { get; set; }
        public string ReasonDescription { get; set; }
        public Decimal TotalQuantity { get; set; }
        public DateTime TimeStamp { get; set; }
        public int Type { get; set; }
        public virtual Store Store { get; set; }
        public virtual ICollection<GoodsNoteDetail> GoodsNoteDetails { get; set; }
    }
}
