using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class ShippingDetail
    {
        [Key]
        public string Id { get; set; }
        public string Address { get; set; }
        public DateTime? ShippingDate { get; set; }
        public string? PhoneReceive { get; set; }
        public DateTime EstimatedArrival { get; set; }
        public int? TransactionPaymentType { get; set; }
        [ForeignKey(nameof(Invoice))]
        public string InvoiceId { get; set; }
        [ForeignKey(nameof(ApplicationUser))]
        public string ShipperId { get; set; }
        public ApplicationUser Shipper{ get; set; }
        public Invoice Invoice { get; set; }
    }
}
