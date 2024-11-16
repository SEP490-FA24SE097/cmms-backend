using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class Import
    {
        [Key]
        public Guid Id { get; set; }
        public Guid? SupplierId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalDiscount { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<ImportDetail> ImportDetails { get; set; }
    }
}
