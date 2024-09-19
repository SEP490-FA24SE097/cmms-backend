using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    [PrimaryKey(nameof(UserId), nameof(PermissionId))]
    public class UserPermission
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}
