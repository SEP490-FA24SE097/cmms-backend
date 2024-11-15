using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    public class Unit
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Material> Materials { get; set; }
        public virtual ICollection<ConversionUnit> ConversionUnits { get; set; }
    }
}
