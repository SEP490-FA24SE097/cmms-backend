
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

    public class InvoiceVM
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
        public UserVM? UserVM { get; set; }
        public List<InvoiceDetailVM>? InvoiceDetails { get; set; }
        public ShippingDetaiInvoicelVM? shippingDetailVM { get; set; }
    }

    public class InvoiceFitlerModel
    {
        public string? Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerId { get; set; }
        public string? InvoiceId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public InvoiceFitlerModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }

    public class InvoiceDetailFitlerModel
    {
        public string? InvoiceId { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public InvoiceDetailFitlerModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }

    public class InvoiceDetailVM : CartItemVM
    {

    }
 
}
