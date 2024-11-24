using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Warehouse
    {
        [Key]
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal? InRequestQuantity { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public virtual Material Material { get; set; }
        public virtual Variant? Variant { get; set; }
    }
}
