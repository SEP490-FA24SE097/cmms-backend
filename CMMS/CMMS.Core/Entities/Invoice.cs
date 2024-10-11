using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Invoice
    {
        [Key]
        public string Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public double TotalAmount { get; set; }
        public int InvoiceStatus { get; set; }
        public string? Note { get; set; }
        public int Status { get; set; }
        [ForeignKey(nameof(ApplicationUser))]
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }
    }
}
