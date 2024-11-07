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
        public string BarCode { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public string Supplier { get; set; }
        public string Description { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinStock { get; set; }
        public string Brand { get; set; }
        public bool IsRewardEligible { get; set; }
        public string ImageUrl { get; set; }

        public List<SubImageDTO>? SubImages { get; set; }
    }

    public class SubImageDTO
    {
        public Guid Id { get; set; }
        public string SubImageUrl { get; set; }

    }
}
