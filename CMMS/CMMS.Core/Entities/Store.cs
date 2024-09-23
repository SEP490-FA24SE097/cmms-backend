

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CMMS.Core.Entities
{
    public class Store
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; } 
        public string Phone { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        [DefaultValue(1)]
        public int Status { get; set; }
    }
}
