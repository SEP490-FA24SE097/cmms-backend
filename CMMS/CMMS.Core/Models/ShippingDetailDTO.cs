using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class ShippingDetailDTO
    {
        public string Id { get; set; }
        public string? Address { get; set; }
        public string? PhoneReceive { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public int? TransactionPaymentType { get; set; }
        public string? InvoiceId { get; set; }
        public string? ShipperId { get; set; }
    }

    public class ShippingDetailVM
    {
        public string? Id { get; set; }
        public string Address { get; set; }
        public string? PhoneReceive { get; set; }
        public int? TransactionPaymentType { get; set; }
        public string? TransactionPayment { get
            {
                if (TransactionPaymentType.Equals((int)Enums.TransactionPaymentType.COD))
                    return "Tiền mặt - COD";
                 else if (TransactionPaymentType.Equals((int)Enums.TransactionPaymentType.OnlinePayment))  
                    return "Chuyển khoản";
                return null;
            }
        }
        public DateTime? ShippingDate { get; set; }
        public DateTime EstimatedArrival { get; set; }
        public string? ShipperName { get; set; }
        public string? ShipperCode { get; set; }
        public InvoiceShippingDetailsVM Invoice { get; set; }
    }

    public class ShippingDetaiInvoicelVM
    {
        public string? Id { get; set; }
        public string Address { get; set; }
        public string? PhoneReceive { get; set; }
        public string? ShipperName { get;set; }
        public string? ShipperCode { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime EstimatedArrival { get; set; }
    }

    public class ShippingDetailFilterModel
    {
        public string? InvoiceId { get; set; }
        public string? ShippingDetailCode { get; set; }
        public string? ShipperId { get; set; }
        public int? InvoiceStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public ShippingDetailFilterModel() { 
            defaultSearch = new DefaultSearch();
        }
    }


    public class ShipperFilterModel
    {
        public string? InvoiceId { get; set; }
        public string? ShipperId { get; set; }
        public string? StoreId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public ShipperFilterModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }


}
