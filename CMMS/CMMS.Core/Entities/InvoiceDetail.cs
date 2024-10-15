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
        public int Quantity { get; set; }
        public double LineTotal { get; set; }
        public virtual ICollection<Material> Materials { get; set; }
        public virtual ICollection<Variant>? Variants { get; set; }
        public Invoice Invoice { get; set; }
    }
}
