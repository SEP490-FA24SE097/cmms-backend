using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class ConversionDTO:ConversionUnit
    {
        public decimal CostPrice { get; set; }  
    }
}
