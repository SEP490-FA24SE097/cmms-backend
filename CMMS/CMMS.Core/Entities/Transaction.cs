using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Transaction
    {
        [Key]
        public string Id { get; set; }
        public int TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? InvoiceId { get; set; }
        [ForeignKey(nameof(Customer))]
        public string CustomerId { get; set; }
        public string? CreatedById { get; set; }
        // cash or payment online
        public int? TransactionPaymentType { get; set; }
        public ApplicationUser Customer { get; set; }
        public Invoice? Invoice { get; set; }
    }
}
