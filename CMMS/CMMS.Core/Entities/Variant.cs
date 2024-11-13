using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    [Index(nameof(SKU), IsUnique = true)]
    public class Variant
    {
        [Key]
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string VariantImageUrl { get; set; }
        public virtual Material Material { get; set; }
        public virtual ICollection<MaterialVariantAttribute> MaterialVariantAttributes { get; set; }
        public virtual ICollection<Import> Imports { get; set; }
        public virtual ICollection<Warehouse> Warehouses { get; set; }
    }
}
