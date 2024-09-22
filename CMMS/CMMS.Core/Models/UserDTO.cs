using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class UserDTO
    {
        public string FullName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, MinLength(6)]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        public string DOB { get; set; }
        [StringLength(10)]
        public string PhoneNumber { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Address { get; set; }
        public string Note { get; set; } = string.Empty;
        public int Status { get; set; } = 1;

    }

    public class UserCM : UserDTO
    {
        public string RoleName { get; set; }
    }

    public class UserRolesVM
    {
        public string Id { get; set; }
        public string FullName { get; set; }

        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DOB { get; set; }
        public string PhoneNumber { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Address { get; set; }
        public string Note { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
        public List<string> RolesName { get; set; }

    }

    public class UserSignIn
    {
        public String UserName { get; set; }
        public String Password { get; set; }
    }

    public class UserSignInVM : UserSignIn
    {
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class UserVM
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string DOB { get; set; }
        public string PhoneNumber { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Address { get; set; }
        public string Note { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
    }

    public class UserRoles : ApplicationUser
    {
        public List<string> RolesName { get; set; }
    }
}
