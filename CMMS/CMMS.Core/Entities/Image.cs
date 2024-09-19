using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Image
    {
        [Key]
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsMainImage { get; set; }
        public virtual Material Material { get; set; }
    }
}
