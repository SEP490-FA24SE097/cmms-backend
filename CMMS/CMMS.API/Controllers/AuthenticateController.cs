using AutoMapper;
using CMMS.API.Services;
using CMMS.Core.Constant;
using CMMS.Core.Models;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using System.Text.Json;
using CMMS.API.Constant;
using Newtonsoft.Json;
using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using NuGet.Common;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller;
using CMMS.Infrastructure.Enums;

namespace CMMS.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly HttpClient _httpClient;
        private readonly ICartService _cartService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMailService _mailService;
        private readonly IMapper _mapper;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthenticateController(IJwtTokenService jwtTokenService,
            IUserService userService,
            ICurrentUserService currentUserService,
            IMapper mapper, HttpClient httpClient,
            ICartService cartService, IUnitOfWork unitOfWork,
            IMailService mailService)
        {
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _currentUserService = currentUserService;
            _httpClient = httpClient;
            _cartService = cartService;
            _unitOfWork = unitOfWork;
            _mailService = mailService;

        }

        [AllowAnonymous]
        [HttpPost("signUp")]
        public async Task<IActionResult> SignUp(UserDTO signUpModel)
        {
            var emailExist = await _userService.FindbyEmail(signUpModel.Email);
            var userNameExist = await _userService.FindByUserName(signUpModel.UserName);
            if (emailExist != null)
            {
                return BadRequest("Email đã được sử dụng");
            }
            else if (userNameExist != null)
            {
                return BadRequest("User đã được sử dụng");
            }

            if (signUpModel.TaxCode != null)
            {
                var taxCode = signUpModel.TaxCode;
                var apiUrl = "https://api.vietqr.io/v2/business/{taxCode}";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to fetch taxCode api checking");
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<TaxCodeCheckApiResponse>(responseContent);

                if (apiResponse.Code == "00")
                {
                    signUpModel.Type = (int)CustomerType.Agency;
                    var resultCreate = await _userService.CustomerSignUpAsync(signUpModel);
                    if (resultCreate.Succeeded)
                        return Ok(resultCreate.Succeeded);
                }
                else
                {
                    return BadRequest(apiResponse.Desc);
                }
            }
            signUpModel.Type = (int)CustomerType.Customer;
            var result = await _userService.CustomerSignUpAsync(signUpModel);
            var url = Url.Action(nameof(ConfirmAccount), nameof(AuthenticateController).Replace("Controller", ""), null, Request.Scheme);
            if (result.Succeeded)
                url += $"?email={signUpModel.Email}";
                await _mailService.SendEmailAsync(signUpModel.Email, "Xác thực tài khoản của bạn", url);
            return Ok(new
            {
                data = result.Succeeded,
                pagination = new
                {
                    total = 0,
                    perPage = 0,
                    currentPage = 0,
                },
            });
        }

        [AllowAnonymous]
        [HttpPost("signIn")]
        public async Task<IActionResult> SignIn(UserSignIn signIn)
        {
            var user = await _userService.SignInAsync(signIn);
            if (user == null)
            {
                return NotFound("Sai tên đăng nhập hoặc mật khẩu");
            }
            else if (user.Status == 0)
            {
                return BadRequest("Tài khoản của bạn bị vô hiệu hóa");
            }
            else if (user.EmailConfirmed == false)
            {
                return BadRequest("Tài khoản của bạn chưa được xác nhận vui lòng confirm qua email của bạn!");
            }

            var userRoles = await _userService.GetRolesAsync(user);
            var accessToken = await _jwtTokenService.CreateToken(user, userRoles);
            var refreshToken = _jwtTokenService.CreateRefeshToken();
            user.RefreshToken = refreshToken;
            user.DateExpireRefreshToken = DateTime.Now.AddDays(7);
            _userService.Update(user);
            var userVM = _mapper.Map<UserVM>(user);
            var result = await _userService.SaveChangeAsync();
            if (result)
            {
                return Ok(new
                {
                    data = new
                    {
                        accessToken,
                        refreshToken, 
                        user = userVM
                    },
                    pagination = new
                    {
                        total = 0,
                        perPage = 0,
                        currentPage = 0,
                    }
                });
            }
            return BadRequest("Failed to update user's token");
        }

        [HttpDelete("signOut")]
        public async Task<IActionResult> SignOut()
        {
            var user = await _currentUserService.GetCurrentUser();
            if (user is null)
                return Unauthorized();
            user.RefreshToken = null;
            _userService.Update(user);
            await _userService.SaveChangeAsync();
            return Ok();
        }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> refeshToken(string refreshToken)
        {
            var userId = _currentUserService.GetUserId();
            var user = await _userService.FindAsync(Guid.Parse(userId));
            if (user == null || !(user.Status != 0) || user.RefreshToken != refreshToken || user.DateExpireRefreshToken < DateTime.UtcNow)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return BadRequest(new Message
                {
                    Content = "Not permission",
                    StatusCode = 404
                });
            }
            var userRoles = await _userService.GetRolesAsync(user);
            var newRefreshToken = _jwtTokenService.CreateRefeshToken();
            user.RefreshToken = newRefreshToken;
            user.DateExpireRefreshToken = DateTime.Now.AddDays(7);
            var accessToken = await _jwtTokenService.CreateToken(user, userRoles);
            _userService.Update(user);
            await _userService.SaveChangeAsync();
            return Ok(new
            {
                data = new
                {
                    accessToken,
                    refreshToken = newRefreshToken
                },
                pagination = new
                {
                    total = 0,
                    perPage = 0,
                    currentPage = 0,
                }
            }
                );
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmAccount([FromQuery] string email)
        {
            var result = await _userService.ConfirmAccount(email);
            if (result) return Ok(result);
            else return BadRequest("Xác thực email không thành công");
        }

    }
}
