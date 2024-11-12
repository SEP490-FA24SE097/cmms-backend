using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class TransactionDTO
    {
        public string Id { get; set; }
        public int TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? InvoiceId { get; set; }
        public string? CustomerId { get; set; }
    }

    public enum TransactionTypeData
    {
        DebtInvoice = 0,
        DebtPurchase = 1,
        Cash = 2,
        OnlinePayment = 3,
    }

    public class TransactionVM
    {
        public string Id { get; set; }
        public int TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? InvoiceId { get; set; }
        public string? CustomerId { get; set; }
        public string Description { 
            get {
                if(TransactionType.Equals((int)TransactionTypeData.DebtPurchase) && InvoiceId != null) {
                    var number = Math.Abs(BitConverter.ToInt32(Guid.Parse(Id).ToByteArray(), 0));
                    return $"Thanh toán tiền nợ cho hóa đơn: {number:D4}";
                };
                if (TransactionType.Equals((int)TransactionTypeData.DebtPurchase) && InvoiceId == null)
                {
                    var number = Math.Abs(BitConverter.ToInt32(Guid.Parse(Id).ToByteArray(), 0));
                    return $"Thanh toán tiền nợ - giá: {Amount}";
                };
                if (TransactionType.Equals((int)TransactionTypeData.DebtInvoice) && InvoiceId != null)
                {
                    var number = Math.Abs(BitConverter.ToInt32(Guid.Parse(Id).ToByteArray(), 0));
                    return $"Tạo hóa đơn nợ cho hóa đơn: {number:D4} - giá: {Amount} ";
                };
                if (TransactionType.Equals((int)TransactionTypeData.Cash))
                {
                    var number = Math.Abs(BitConverter.ToInt32(Guid.Parse(Id).ToByteArray(), 0));
                    return $"Tạo hóa đơn: {number:D4} - giá: {Amount} ";
                };
                if (TransactionType.Equals((int)TransactionTypeData.OnlinePayment))
                {
                    var number = Math.Abs(BitConverter.ToInt32(Guid.Parse(Id).ToByteArray(), 0));
                    return $"Thanh toán online cho hóa đơn: {number:D4} - giá: {Amount} ";
                };
                return "123";
            } 
        }
        public InvoiceTransactionVM? InvoiceVM{ get; set; }
        public UserVM? UserVM{ get; set; }
    }

    public class InvoiceTransactionVM
    {
        public string Id { get; set; }
        public string InvoiceCode
        {
            get
            {
                // Chuyển GUID thành một chuỗi định dạng số và thêm tiền tố "ĐH"
                var number = Math.Abs(BitConverter.ToInt32(Guid.Parse(Id).ToByteArray(), 0));
                return $"ĐH{number:D4}"; // Ví dụ: ĐH0001, ĐH0002
            }
        }
        public DateTime InvoiceDate { get; set; }
        public double TotalAmount { get; set; }
        public int InvoiceStatus { get; set; }
        public string? Note { get; set; }
        public List<InvoiceDetailVM>? InvoiceDetails { get; set; }
    }
    
    public class TransactionFilterModel
    {
        public string? TransactionId { get; set; } 
        public string? InvoiceId { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public TransactionFilterModel()
        {
            defaultSearch = new DefaultSearch();
        }

    }
}
