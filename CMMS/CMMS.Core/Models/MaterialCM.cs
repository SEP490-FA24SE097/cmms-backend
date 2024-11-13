using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialCM
    {
        public string Name { get; set; }
        public string BarCode { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UnitId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid BrandId { get; set; }
        public string Description { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal MinStock { get; set; }
        public bool IsRewardEligible { get; set; }
        public string ImageUrl { get; set; }
    }
}
