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
    public class Supplier
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }    
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        
    }
}
