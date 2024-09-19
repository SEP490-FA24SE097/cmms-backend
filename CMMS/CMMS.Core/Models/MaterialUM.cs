using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialUM
    {
        public string? Name { get; set; }
        public string? CategoryId { get; set; }
        public string? UnitId { get; set; }
        public string? SupplierId { get; set; }
        public string? Description { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinStock { get; set; }
        public bool IsRewardEligible { get; set; }
    }
}
