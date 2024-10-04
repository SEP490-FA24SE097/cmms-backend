using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Core.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    public class Category

    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category>? Categories { get; set; }
        public virtual ICollection<Material> Materials { get; set; }
    }
}
