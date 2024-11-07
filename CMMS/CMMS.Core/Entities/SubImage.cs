using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class SubImage
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string SubImageUrl { get; set; }
        public virtual Material Material { get; set; }
    }
}
