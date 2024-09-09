using Microsoft.AspNetCore.Identity;

namespace CMMS.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime? DOB { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String PhoneNumber { get; set; }
        public bool Gender { get; set; }
        public int Status { get; set; }
        public string? Avatar { get; set; }
        public String? RefreshToken { get; set; }
        public DateTime? DateExpireRefreshToken { get; set; }
    }
}
