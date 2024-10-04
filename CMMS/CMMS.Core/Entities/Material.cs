using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    [Index(nameof(Name),IsUnique = true)]
    public class Material
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UnitId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid BrandId { get; set; }
        public string Description { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinStock { get; set; }
        public string ImageUrl { get; set; }
        public bool IsRewardEligible { get; set; }
        public virtual Category Category { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual Brand Brand { get; set; }
        
        public virtual ICollection<Variant> Variants { get; set; }

    }
}
