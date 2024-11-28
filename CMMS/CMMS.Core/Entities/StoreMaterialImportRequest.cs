using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class StoreMaterialImportRequest
    {
        [Key]
        public Guid Id { get; set; }
        public string StoreId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public string? FromStoreId { get; set; }
        public decimal Quantity { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public virtual Material Material { get; set; }
        public virtual Variant? Variant { get; set; }
        public virtual Store Store { get; set; }
        public virtual Store? FromStore { get; set; }

    }
}
