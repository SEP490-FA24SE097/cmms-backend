using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CMMS.Core.Models
{
    public class PricedMaterialDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialName { get; set; }
        public string MaterialImage { get; set; }
        public Guid? VariantId { get; set; }
        public string? VariantName { get; set; }
        public string? VariantImage { get; set; }
        public decimal LastImportPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellPrice { get; set; }
    }
}
