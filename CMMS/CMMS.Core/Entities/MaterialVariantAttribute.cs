using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class MaterialVariantAttribute
    {
        [Key]
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }
        public Guid AttributeId { get; set; }
        public string Value { get; set; }
        public virtual Variant Variant { get; set; }
        public virtual Attribute Attribute { get; set; }

    }
}
