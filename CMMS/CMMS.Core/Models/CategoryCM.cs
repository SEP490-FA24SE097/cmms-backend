using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class CategoryCM
    {
        
        public string Name { get; set; }
        public Guid ParentCategoryId { get; set; }
    }
}
