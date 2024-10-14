
using CMMS.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMMS.Core.Models
{
    public class InvoiceDTO
    {
        public string Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public double TotalAmount { get; set; }
        public int InvoiceStatus { get; set; }
        public string? Note { get; set; }
        public string CustomerId { get; set; }
    }
}
