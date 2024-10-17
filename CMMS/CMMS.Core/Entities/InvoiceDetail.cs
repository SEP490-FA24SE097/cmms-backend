using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class InvoiceDetail
    {
        [Key]
        public string Id { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal Quantity { get; set; }
        public decimal LineTotal { get; set; }
        [ForeignKey(nameof(Invoice))]
        public string InvoiceId { get; set; }   
        public virtual ICollection<Material> Materials { get; set; }
        public virtual ICollection<Variant>? Variants { get; set; }
        public virtual Invoice Invoice { get; set; }
	}
}
