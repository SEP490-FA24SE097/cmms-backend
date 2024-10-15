using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialVariantDTO
    {
        public MaterialDTO Material { get; set; }
        public List<VariantDTO> Variants { get; set; }

    }

    public class VariantDTO
    {
        public Guid VariantId { get; set; }
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public List<AttributeDTO> Attributes { get; set; }
    }

    public class AttributeDTO
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }
}
