using CMMS.Core.Entities;
using CMMS.Infrastructure.Constant;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CMMS.API.Services
{
    public interface ICurrentUserService
    {
        string GetUserId();
        string getUserEmail();
        Task<ApplicationUser> GetCurrentUser();
        Task<String> GetUserAddress();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public string GetUserId() =>
             _httpContextAccessor.HttpContext.
				User.Claims.FirstOrDefault(_ => _.Type == CustomClaims.UserId)?.Value;

        public string GetUserStoreId() =>
     _httpContextAccessor.HttpContext.
        User.Claims.FirstOrDefault(_ => _.Type == CustomClaims.StoreId)?.Value;

        public string getUserEmail()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        public async Task<ApplicationUser> GetCurrentUser()
        {
            var userId = GetUserId();
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task<string> GetUserAddress()
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return $"{user.Address}, {user.Ward}, {user.District}, {user.Province}";
        }
    }
}
