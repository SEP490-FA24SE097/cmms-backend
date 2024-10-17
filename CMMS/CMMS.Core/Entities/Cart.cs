using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Cart
    {
        [Key]
        public string Id { get; set; }
        [ForeignKey(nameof(Material))]
        public Guid MaterialId { get; set; }
        [ForeignKey(nameof(Variant))]
        public Guid? VariantId { get; set; }
        [ForeignKey(nameof(ApplicationUser))]
        public string CustomerId { get; set; }
        public decimal Quantity { get; set; }    
        public decimal TotalAmount { get; set; } 
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public virtual Material Materials { get; set; }
        public virtual Variant? Variants{ get; set; }
        public virtual ApplicationUser Customer { get; set; }
    }
}
