using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class StoreDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public int Status { get; set; }
    }


    public class StoreCM
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public int Status { get; set; }
    }


    public class StoreDistance 
    {
        public Store Store { get; set; }
        public decimal Distance { get; set; }
    }


    public class StoreVM : StoreDTO
    {
        public UserVM Manager { get; set; }
    }

}
