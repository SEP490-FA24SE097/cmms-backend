using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class RoleDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class RolePermissions
    {
        public string RoleName { get; set; }
        public String[] Permissions { get; set; }    
    }
}
