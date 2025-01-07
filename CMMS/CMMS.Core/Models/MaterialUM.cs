using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialUM
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public float? WeightValue { get; set; }
        public string? BarCode { get; set; }
        public string? CategoryId { get; set; }
        public string? BrandId { get; set; }
        public string? Description { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public List<string>? ImageFiles { get; set; }
        public bool? isPoint { get; set; }
    }
}
