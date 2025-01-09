using CMMS.Core.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class MaterialCM
    {
        public class PostMaterialRequest
        {
            public string? Barcode { get; set; }
            public string Name { get; set; }
            public decimal CostPrice { get; set; }
            public decimal SalePrice { get; set; }
            public string MainImage { get; set; }
            public List<string> SubImages { get; set; }
            public float? WeightValue { get; set; }
            public string? Description { get; set; }
            public decimal MinStock { get; set; }
            public decimal MaxStock { get; set; }
            public bool? IsPoint { get; set; } = false;
            public Guid BasicUnitId { get; set; }
            public List<MaterialUnitDto>? MaterialUnitDtoList { get; set; }
            public Guid CategoryId { get; set; }
            public Guid BrandId { get; set; }
        }

        public class MaterialUnitDto
        {
            public Guid UnitId { get; set; }
            public decimal ConversionRate { get; set; }
            public decimal Price { get; set; }
        }

    }
}
