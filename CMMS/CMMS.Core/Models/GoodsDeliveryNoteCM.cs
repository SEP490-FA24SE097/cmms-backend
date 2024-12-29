using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class TrackingCM
    {
        public string? StoreId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public Decimal QuantityInSystem { get; set; }
        public Decimal QuantityInReality { get; set; }
    }


}
