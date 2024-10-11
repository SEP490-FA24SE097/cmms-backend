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
        [ForeignKey(nameof(Material))]
        public string MaterialId { get; set; }
        [ForeignKey(nameof(Variant))]
        public string? VariantId { get; set; }
        public int Quantity { get; set; }
        public double LineTotal { get; set; }
        public ICollection<Material> Materials { get; set; }
        public virtual Variant? Variants { get; set; }
        public Invoice Invoice { get; set; }
    }
}
