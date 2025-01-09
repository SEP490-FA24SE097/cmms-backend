using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CMMS.Core.Entities;

namespace CMMS.Core.Models
{
    public class WarehouseDTO
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialImage { get; set; }
        public Guid? VariantId { get; set; }
        public string VariantName { get; set; }
        public string VariantImage { get; set; }
        public string Brand { get; set; }
        public float? Weight { get; set; }
        public string ParentCategory { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public string? Supplier { get; set; }
        public decimal Quantity { get; set; }
        public decimal? MinStock { get; set; }
        public decimal? MaxStock { get; set; }
        [JsonIgnore]
        public decimal? InOrderQuantity { get; set; }
        public decimal? MaterialPrice { get; set; }
        public decimal? MaterialCostPrice { get; set; }
        public decimal? VariantPrice { get; set; }
        public decimal? VariantCostPrice { get; set; }
        public string? Discount { get; set; }
        public decimal? AfterDiscountPrice { get; set; }
        public List<AttributeDTO>? Attributes { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public class InventoryDTO : WarehouseDTO
    {
        public decimal? AutoImportQuantity { get; set; }
    }
}
