using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    public class GoodsNoteDetail
    {
        [Key]
        public Guid Id { get; set; }
        public Guid GoodsNoteId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public Decimal Quantity { get; set; }
        public virtual Material  Material { get; set; }
        public virtual Variant? Variant { get; set; }
        public virtual GoodsNote GoodsNote { get; set; }
    }
}
