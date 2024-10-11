using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMMS.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime? DOB { get; set; }
        public string? PhoneNumber { get; set; }
        public int Status { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }
        public String? RefreshToken { get; set; }
        public string? TaxCode { get; set; }
        public int Type { get; set; }
        public double? CreditLimit { get; set; }
        public DateTime? DateExpireRefreshToken { get; set; }
        [ForeignKey(nameof(Store))]
        public string? StoreId { get; set; }
        public Store? Store { get; set; }
        public ICollection<Invoice>? Invoices { get; set; }
        public CustomerBalance? CustomerBalance { get; set; }

    }
}