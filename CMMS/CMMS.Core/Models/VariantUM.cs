using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class VariantUM
    {
        public Guid Id { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string VariantImage { get; set; }

    }
}
