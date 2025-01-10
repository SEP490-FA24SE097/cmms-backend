using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{

    public class ShippingConfigurationFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public ShippingConfigurationFilterModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }

    public class ShippingCofigDTO {
        public string? Id { get; set; }
        public decimal BaseFee { get; set; }
        public decimal First5KmFree { get; set; }
        public decimal AdditionalKmFee { get; set; }
        public decimal First10KgFee { get; set; }
        public decimal AdditionalKgFee { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CustomerDiscountConfigurationFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public CustomerDiscountConfigurationFilterModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }

    public class CustomerDiscountCofigDTO
    {
        public string? Id { get; set; }
        public decimal Customer { get; set; }
        public decimal Agency { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }
    }


}
