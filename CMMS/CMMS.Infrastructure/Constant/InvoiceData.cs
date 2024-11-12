using CMMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Constant
{
    public class InvoiceData
    {
        public string? Note { get; set; }
        public string? Address { get; set; }
        public PaymentType PaymentType { get; set; }
        public List<CartItem>? CartItems { get; set; }
        public decimal? Amount { get; set; }
        public string? InvoiceId { get; set; }
        public string? CustomerId { get; set; }
    }
    public enum PaymentType
    {
        DebtInvoice = 0,
        DebtPurchase = 1,
        PurchaseFirst = 2,
        PurchaseAfter = 3,
        OnlinePayment = 4,
    }
}
