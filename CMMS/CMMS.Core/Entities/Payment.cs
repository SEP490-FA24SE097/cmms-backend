using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Payment
    {
        [Key]
        public string Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }
        [ForeignKey(nameof(Invoice))]
        public string? InvoiceId { get; set; }
        public int PaymentStatus { get; set; }
        public string PaymentDescription { get; set; }
        public string? BankCode { get; set; }
		public Invoice? Invoice { get; set; }
    }
}
