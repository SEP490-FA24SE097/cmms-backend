using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class WarehouseMinMaxStockUM
    {
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal? MinStock { get; set; }
        public decimal? MaxStock { get; set; }
    }
    public class StoreMinMaxStockUM : WarehouseMinMaxStockUM
    {
        public string StoreId { get; set; }
    }
    public class AutoImportQuantityUM 
    {
        public string StoreId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal? ImportQuantity { get; set; }
    }
}
