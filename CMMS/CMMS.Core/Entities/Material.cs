using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    public class Material
    {
        [Key]
        public Guid Id { get; set; }  
        public string? MaterialCode { get; set; }  
        public string? BarCode { get; set; }  
        public string? Name { get; set; }  
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }  
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }  
        public float? WeightValue { get; set; }  
        public string? WeightUnit { get; set; }  
        public string? Description { get; set; }  
        public decimal MinStock { get; set; }  
        public decimal MaxStock { get; set; }  
        public string? ImageUrl { get; set; }  
        public bool IsRewardEligible { get; set; } 
        public bool? IsActive { get; set; } = true;  
        public Guid UnitId { get; set; }
        public DateTime Timestamp { get; set; }
        public virtual Unit Unit { get; set; }  
        public virtual ICollection<ConversionUnit> ConversionUnits { get; set; }  
        public Guid CategoryId { get; set; }  
        public virtual Category Category { get; set; } 
        public Guid BrandId { get; set; }  
        public virtual Brand Brand { get; set; }  
        public virtual ICollection<Variant> Variants { get; set; }
        public virtual ICollection<Import> Imports { get; set; }
        public virtual ICollection<Warehouse> Warehouses { get; set; }
        public virtual ICollection<SubImage> SubImages { get; set; }
    }
}

