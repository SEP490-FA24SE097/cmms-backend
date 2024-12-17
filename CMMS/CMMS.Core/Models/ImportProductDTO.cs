using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class ImportProductDTO
    {
        public Guid MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public string? Image { get; set; }
        public Guid? VariantId { get; set; }
        public string? Sku { get; set; }
        public string? VariantImage { get; set; }
        public decimal? VariantSalePrice { get; set; }
        public decimal? VariantCostPrice { get; set; }
    }
}
