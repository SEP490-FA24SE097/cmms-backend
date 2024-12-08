using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class ConversionUnitCM
    {
        public Guid UnitId { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Price { get; set; }
    }
}
