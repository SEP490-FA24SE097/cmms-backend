using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class ApproveDTO
    {
        public string? FromStoreId { get; set; }
        public Guid RequestId { get; set; }
        public bool IsApproved { get; set; }
    }
    public class ConfirmDTO
    {
        public Guid RequestId { get; set; }
        public bool IsConfirmed { get; set; }
    }
    public class CancelDTO
    {
        public Guid RequestId { get; set; }
    }
}
