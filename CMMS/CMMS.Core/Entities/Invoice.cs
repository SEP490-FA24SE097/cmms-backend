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
        public decimal TotalAmount { get; set; }
        // giam gias theo so luong tien cua custoemr Type.
        public decimal? Discount { get; set; }
        public decimal? SalePrice { get; set; }
        // khach hang da~ tra bao nhieu tien cua hoa don do'
        public decimal? CustomerPaid { get; set; }

        public int InvoiceType { get; set; }
        public int InvoiceStatus { get; set; }
        // website or in store
        public int SellPlace { get; set; }
        public string? Note { get; set; }
        [ForeignKey("CustomerId")]
        public string CustomerId { get; set; }
        // query nguoi ban. tai store.
        public string? StaffId { get; set; } 
        public string? StoreId { get;set; }    
        public ApplicationUser Customer { get; set; }
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }
    }
}
