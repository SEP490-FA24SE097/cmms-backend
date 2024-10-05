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

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalLogin : ControllerBase
    {
        private SignInManager<ApplicationUser> _signInManager;

        public ExternalLogin(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        [HttpGet("google-signin")]
        public IActionResult GoogleLogin()
        {
            var state = Guid.NewGuid().ToString();
            Response.Cookies.Append("state", state);

            var provider = "Google";
            var redirectUrl = Url.Action(nameof(GoogleCallBack), "ExternalLogin");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            properties.Items.Add("state", state);
            properties.AllowRefresh = true;
            return new ChallengeResult(provider, properties);
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleCallBack(string state, string code)
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

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return Redirect("/");
            }
            if (result.IsLockedOut)
            {
                return BadRequest();
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                //ReturnUrl = returnUrl;
                //LoginProvider = info.LoginProvider;
                var Email = "";
                var Name = info.Principal.Identity.Name;

                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Email = info.Principal.FindFirstValue(ClaimTypes.Email);
                }

                var user = new IdentityUser { UserName = Email, Email = Name };
                //var result2 = await _userManager.CreateAsync(user);
                //if (result2.Succeeded)
                //{
                //    result2 = await _userManager.AddLoginAsync(user, info);
                //    if (result2.Succeeded)
                //    {
                //        await _signInManager.SignInAsync(user, isPersistent: false);
                //        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                //        var userId = await _userManager.GetUserIdAsync(user);
                //        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                //        var callbackUrl = Url.Page(
                //            "/Account/ConfirmEmail",
                //            pageHandler: null,
                //            values: new { area = "Identity", userId = userId, code = code },
                //            protocol: Request.Scheme);

                //        await _emailSender.SendEmailAsync(Email, "Confirm your email",
                //            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                //        return Redirect("/");
                //    }
                //}
                //foreach (var error in result2.Errors)
                //{
                //    ModelState.AddModelError(string.Empty, error.Description);
                //}

                return Redirect("/");
            }


        }
    }
}
