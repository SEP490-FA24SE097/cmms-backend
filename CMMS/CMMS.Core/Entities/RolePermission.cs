using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Entities
{
    [PrimaryKey(nameof(RoleId), nameof(PermissionId))]
    public class RolePermission
    {
        public string RoleId { get; set; }  
        public string PermissionId { get; set; } 
        public IdentityRole Role { get; set; }
        public Permission Permission { get; set; }  
    }
}
