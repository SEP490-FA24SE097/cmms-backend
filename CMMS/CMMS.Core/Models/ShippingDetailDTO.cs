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
        public DateTime? ShippingDate { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string? InvoiceId { get; set; }
    }

    public class ShippingDetailVM
    {
        public string? Id { get; set; }
        public string Address { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime EstimatedArrival { get; set; }
        public Invoice Invoice { get; set; }
    }

    public class ShippingDetaiInvoicelVM
    {
        public string? Id { get; set; }
        public string Address { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime EstimatedArrival { get; set; }
    }

    public class ShippingDetailFilterModel
    {
        public string? InvoiceId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public ShippingDetailFilterModel() { 
            defaultSearch = new DefaultSearch();
        }
    }
}
