using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class VariantCM
    {
        public Guid MaterialId { get; set; }
        public string? SKU { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string VariantImageUrl { get; set; }
        public ICollection<Attribute> Attributes { get; set; }
    }

    public class Attribute
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }
}
