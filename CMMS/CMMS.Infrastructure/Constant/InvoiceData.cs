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
        public string? PhoneReceive { get;set; }
        public decimal? Amount { get; set; }
        public decimal? Discount { get; set; }
        public decimal? SalePrice { get; set; }
        public string? InvoiceId { get; set; }
        public string? CustomerId { get; set; }
    }

    public class InvoiceStoreData
    {
        public string? Note { get; set; }
        public string? Address { get; set; }
        public List<CartItem>? StoreItems { get; set; }
        public string? PhoneReceive { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? Discount { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? CustomerPaid { get; set; }
        public string? InvoiceId { get; set; }
        public string? CustomerId { get; set; }
        public string? ShipperId { get; set; }
        public int? InvoiceType { get; set; }
    }

    public class InvoiceDataUpdateStatus
    {
        public string Reason { get; set; }
        public string InvoiceId { get; set; }
        public List<CartItem>? RefundItems { get; set; }
        public int UpdateType { get; set; }
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
