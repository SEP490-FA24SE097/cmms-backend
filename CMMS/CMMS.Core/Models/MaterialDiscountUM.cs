﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialDiscountUM
    {
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public string Discount { get; set; }
    }
}
