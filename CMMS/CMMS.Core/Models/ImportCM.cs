using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{    public class ImportCM
    {
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid SupplierId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
