using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class ConversionUnit
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Price { get; set; }
        public Guid MaterialId { get; set; }
        public virtual Material Material { get; set; }
    }
}
