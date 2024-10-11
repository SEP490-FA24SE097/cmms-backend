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
        public DateTime EstimatedArrival { get; set; }
        [ForeignKey(nameof(Invoice))]
        public string InvoiceId { get; set; }
        public Invoice Invoice { get; set; }
    }
}
