using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialFilterModel
    {
        public string? NameKeyWord { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? SupplierId { get; set; }
        public decimal? lowerPrice { get; set; }
        public decimal? upperPrice { get; set; }

    }
}
