

namespace CMMS.Core.Models
{
    public class InvoiceDTO
    {
        public string Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public double TotalAmount { get; set; }
        public int InvoiceStatus { get; set; }
        public string? Note { get; set; }
        public string? CustomerId { get; set; }
        public decimal? Discount { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? CustomerPaid { get; set; }
        public string? StaffId { get; set; }
        public string? StoreId { get; set; }
        public int? SellPlace { get; set; }
    }

    public class GroupInvoiceVM
    {
        public double TotalAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public List<InvoiceVM> Invoices { get; set; }

    }
    public class InvoiceVM
    {
        public string Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public double TotalAmount { get; set; }
        public int InvoiceStatus { get; set; }
        public string? Note { get; set; }
        public decimal? Discount { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? CustomerPaid { get; set; }
        public string? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StoreId { get; set; }
        public string? StoreName { get; set; }
        public UserVM? UserVM { get; set; }
        public int? SellPlace { get; set; }
        public string? BuyIn  { get
            {
                if(SellPlace == (int)Enums.SellPlace.Website)
                {
                    return "Website";
                }
                return "Tại cửa hàng";
            }
        }
        public List<InvoiceDetailVM>? InvoiceDetails { get; set; }
        public ShippingDetaiInvoiceResponseVM? shippingDetailVM { get; set; }
    }

    public class InvoiceShippingDetailsVM
    {
        public string Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int InvoiceStatus { get; set; }
        public string? Note { get; set; }
        public double TotalAmount { get; set; }
        public decimal? Discount { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? CustomerPaid { get; set; }
        public decimal? NeedToPay { get; set; }
        public string? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StoreId { get; set; }
        public string? StoreName { get; set; }
        public UserVM? UserVM { get; set; }
        public int? SellPlace { get; set; }
        public string? BuyIn
        {
            get
            {
                if (SellPlace == (int)Enums.SellPlace.Website)
                {
                    return "Website";
                }
                return "Tại cửa hàng";
            }
        }
        public List<InvoiceDetailVM>? InvoiceDetails { get; set; }
    }

    public class InvoiceFitlerModel
    {
        public string? Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerId { get; set; }
        public string? InvoiceId { get; set; }
        public string? StoreId { get; set; }
        public string? StaffId { get; set; }
        public int? InvoiceType { get; set; }
        public int? InvoiceStatus { get; set; }
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

    public class DashboardInvoiceFitlerModel
    {
        public DateTime? SpecificDate { get; set; }
        public int? NearDays { get; set; }
        public int? Year { get; set; }
        public string? StoreId { get; set; }
        
    }

    public class InvoiceDetailVM : CartItemVM
    {

    }
 
}
