using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities.Configurations
{
    public class ConfigCustomerDiscount
    {
        [Key]
        public string Id { get; set; }
        public decimal Customer { get; set; }
        public decimal Agency { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
