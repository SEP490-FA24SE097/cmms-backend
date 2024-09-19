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
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        public bool IsAdmin { get; set; } = false;
        [StringLength(10)]
        public String? DOB { get; set; }
        public String? PhoneNumber { get; set; }

        public String Avatar { get; set; }
        public bool Gender { get; set; }
        // default Status = 1;
        public int Status { get; set; } = 1;

    }


    public class UserRolesVM
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public List<string> RolesName { get; set; }
        public String? DOB { get; set; }
        public String? PhoneNumber { get; set; }
        public bool Gender { get; set; }
        public int Status { get; set; } = 1;
    }

    public class UserSignIn
    {
        public String UserName { get; set; }
        public String Password { get; set; }
    }

    public class UserSignInVM : UserSignIn
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class UserVM
    {
        public String Id { get; set; }
        public DateTime? DOB { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String PhoneNumber { get; set; }
        public String Email { get; set; }
        public String Avatar { get; set; }
        public bool Gender { get; set; }
        public int Status { get; set; }
    }

    public class UserRoles : ApplicationUser
    {
        public List<string> RolesName { get; set; }
    }
}
