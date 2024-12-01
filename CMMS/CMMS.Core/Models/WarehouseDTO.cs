using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class WarehouseDTO
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialImage { get; set; }
        public Guid? VariantId { get; set; }
        public string VariantName { get; set; }
        public string VariantImage { get; set; }
        public decimal Quantity { get; set; }
        [JsonIgnore]
        public decimal? InOrderQuantity { get; set; }
        public decimal? MaterialPrice { get; set; }
        public decimal? VariantPrice { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}
