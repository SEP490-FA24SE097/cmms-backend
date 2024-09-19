using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public string Supplier { get; set; }
        public string Description { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinStock { get; set; }
        public decimal SoldQuantity { get; set; }
        public bool IsRewardEligible { get; set; }
        public ICollection<Image> Images { get; set; }
    }
}
