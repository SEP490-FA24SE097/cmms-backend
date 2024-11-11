using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class CustomerBalance
    {
        [Key]
        public string Id { get; set; }
        public decimal TotalDebt { get; set; } = 0;
        public decimal TotalPaid { get; set; } = 0;
        public decimal Balance { get; set; }
        public string? Note { get;set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [ForeignKey(nameof(ApplicationUser))]
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

    }
}
