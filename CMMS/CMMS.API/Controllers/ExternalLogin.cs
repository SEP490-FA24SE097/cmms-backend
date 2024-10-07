using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using CMMS.Core.Entities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Mono.TextTemplating;
using Microsoft.AspNetCore.Authentication.Cookies;
using CMMS.API.Services;
using CMMS.Infrastructure.Services;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using CMMS.Core.Models;
using AutoMapper;
using CMMS.Infrastructure.Enums;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalLogin : ControllerBase
    {
        private IJwtTokenService _jwtTokenService;
        private IUserService _userService;
        private ICurrentUserService _currentUserSerivce;
        private SignInManager<ApplicationUser> _signInManager;
        private IMapper _mapper;
        private UserManager<ApplicationUser> _userManager;

        public ExternalLogin(SignInManager<ApplicationUser> signInManager, 
            IJwtTokenService jwtTokenService,
            IUserService userService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager)
        {
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _currentUserSerivce = currentUserService;
            _signInManager = signInManager;
            _mapper = mapper;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet("google-signin")]
        public  IActionResult GoogleLogin()
        {
            var provider = "Google";
            var redirectUrl = Url.Action(nameof(GoogleCallBack), "ExternalLogin");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            properties.AllowRefresh = true;
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        [HttpGet("google-authenticated")]
        public async Task<IActionResult> GoogleCallBack(string? state, string? code)
        {
         
            if (code != null)
            {
                //ErrorMessage = $"Error from external provider: {remoteError}";
                return BadRequest();
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                //ErrorMessage = "Error loading external login information.";
                return BadRequest();
            }

            var Email = "";

            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email);
            }
            var Name = info.Principal.Identity.Name;
            // Sign in the user with this external login provider if the user already has a login.
            var authenResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (authenResult.Succeeded)
            {
                // already have acccount
                var user = await _userService.FindbyEmail(Email);
                var userRoles = await _userService.GetRolesAsync(user);
                var accessToken = await _jwtTokenService.CreateToken(user, userRoles);
                var refreshToken = _jwtTokenService.CreateRefeshToken();
                user.RefreshToken = refreshToken;
                user.DateExpireRefreshToken = DateTime.Now.AddDays(7);
                _userService.Update(user);
                var result = await _userService.SaveChangeAsync();
                if (result)
                {
                    return Ok(new { token = accessToken, refreshToken });
                }
            }
            if (authenResult.IsLockedOut)
            {
                return BadRequest();
            }
            else
            {
                // have no account in system
                var user = new ApplicationUser { FullName = Name, Email = Email, UserName = Email };
                var refreshToken = _jwtTokenService.CreateRefeshToken();
                var userCM = _mapper.Map<UserCM>(user);
                userCM.RoleName = Role.Customer.ToString();

                userCM.ProviderKey = info.ProviderKey;
                userCM.ProviderDisplayName = info.ProviderDisplayName;
                userCM.LoginProvider = info.LoginProvider;

                var message = await _userService.AddAsync(userCM);
                if (message.StatusCode == 201) {
                    var userEntity = await _userService.FindbyEmail(Email);
                    var userRoles = await _userService.GetRolesAsync(userEntity);
                    var accessToken = await _jwtTokenService.CreateToken(userEntity, userRoles);
                    return Ok(new { token = accessToken, refreshToken });
                }
                return BadRequest("Failed to update user's token");
            }


        }
    }
}
