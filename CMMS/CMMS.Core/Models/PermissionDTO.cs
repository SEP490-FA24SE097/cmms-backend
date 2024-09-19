using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class PermissionDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }    
    }

    public class UserPermissionDTO
    {
        public string UserId { get; set; }
        public string PermissionId { get; set; }
    }

}
