using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class ImportDetail
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ImportId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitDiscount { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal PriceAfterDiscount { get; set; }
        public string? Note { get; set; }
        public virtual Import Import { get; set; }
        public virtual Material Material { get; set; }
        public virtual Variant? Variant { get; set; }
    }
}
