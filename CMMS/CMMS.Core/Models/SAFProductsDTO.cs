using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class SAFProductsDTO
    {
        public string? NameKeyWord { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? CategoryId { get; set; }    
    }
}
