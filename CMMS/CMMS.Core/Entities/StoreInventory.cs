using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class StoreInventory
    {
        [Key]
        public Guid Id { get; set; }
        public string StoreId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal? InOrderQuantity { get; set; }
        public decimal MinStock { get; set; }
        public decimal MaxStock { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public virtual Material Material { get; set; }
        public virtual Variant? Variant { get; set; }
        public virtual Store Store { get; set; }
    }
}
