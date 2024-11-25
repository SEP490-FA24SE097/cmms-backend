using CMMS.Core.Entities;
using CMMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CMMS.Core.Models
{
    public class UserDTO
    {
        public string? Id { get; set; }
        public string? FullName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        //[Required, MinLength(6)]
        public string? UserName { get; set; }
        //[Required]
        public string? Password { get; set; }
        public string? DOB { get; set; }
        //[StringLength(10)]
        public string? PhoneNumber { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? Address { get; set; }
        public string? TaxCode { get; set; }
        public string? Note { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
        public string? StoreId { get; set; }
        public int? Type { get; set; } = 0;
        public decimal? CreditLimit { get; set; }
        public decimal? CurrentDebt { get; set; }
        public string? CreatedById { get; set; }

    }
    public class UserStoreVM : UserDTO
    {
        public string StoreCreateName { get; set; }
        public string CreateByName { get; set; }
        public string CustomerType
        {
            get
            {
                if (Type == (int)Enums.CustomerType.Customer)
                {
                    return "Khách hàng";
                }
                return "Đại lý";
            }
        }
        public string CustomerStatus
        {
            get
            {
                if (Status == (int)Enums.CustomerStatus.Disable)
                {
                    return "Ngừng hoạt động";
                }
                else if (Status == (int)Enums.CustomerStatus.Active)
                {
                    return "Đang hoạt động";
                }
                else if (Status == (int)Enums.CustomerStatus.BadDebtCredit)
                {
                    return "Nợ xấu";
                }
                return null;
            }
        }

        public decimal? CurrentDebt { get; set; }
        public decimal? TotalSale { get; set; }
        public decimal? TotalSaleAfterRefund { get; set; }

    }

    public class UserCM : UserDTO
    {
        public string? LoginProvider { get; set; }
        public string? ProviderDisplayName { get; set; }
        public string? ProviderKey { get; set; }
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
        public DateTime DOB { get; set; }
        public string PhoneNumber { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }
        public string Note { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
        public string? StoreId { get; set; }
    }

    public class ShipperVM : UserVM
    {
        public string? StoreName { get; set; }
        public string? StoreId { get; set; }
    }

    public class UserRoles : ApplicationUser
    {
        public List<string> RolesName { get; set; }
    }

    public class CustomerFilterModel
    {
        public string? CustomerTrackingCode { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Status { get; set; }
        public int? CustomerType { get; set; }
        // chi nhanh tao
        public string? StoreId { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public CustomerFilterModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }

    public class InvoiceilterModel
    {
        public string? CustomerId { get; set; }
       // 1 hoac 6
        public int? InvoiceStatus { get; set; }
        public DefaultSearch defaultSearch { get; set; }
        public InvoiceilterModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }
}
