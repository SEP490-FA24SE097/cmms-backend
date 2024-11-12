using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Import
    {
        [Key]
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid SupplierId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime TimeStamp { get; set; }
        public virtual Material Material { get; set; }
        public virtual Variant? Variant { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}
