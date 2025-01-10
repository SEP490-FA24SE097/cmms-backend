using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CMMS.Core.Entities.Configurations
{
    public class ConfigShipping
    {
        [Key]
        public string Id { get; set; }
        public decimal BaseFee { get; set; }
        public decimal First5KmFree { get; set; }
        public decimal AdditionalKmFee { get; set; }
        public decimal First10KgFee { get; set; }
        public decimal AdditionalKgFee { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }

    }
}
