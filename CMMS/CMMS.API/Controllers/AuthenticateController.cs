using AutoMapper;
using CMMS.API.Services;
using CMMS.Core.Constant;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthenticateController(IJwtTokenService jwtTokenService, IUserService userService, ICurrentUserService currentUserService
            , IMapper mapper)
        {
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _currentUserService = currentUserService;
        }

        [AllowAnonymous]
        [HttpPost("signUp")]
        public async Task<IActionResult> SignUp(UserDTO signUpModel)
        {
            var emailExist = await _userService.FindbyEmail(signUpModel.Email);
            var userNameExist = await _userService.FindByUserName(signUpModel.UserName);
            if (emailExist != null)
            {
                return BadRequest("Email đã tồn tại trên hệ thống!");
            }
            else if (userNameExist != null)
            {
                return BadRequest("Username này đã tồn tại trên hệ thống!");
            }
            var result = await _userService.CustomerSignUpAsync(signUpModel);
            if (result == null)
            {
                return BadRequest("Đăng kí thất bại");
            }
            if (result.Succeeded)
            {
                return Ok(result.Succeeded);
            }

            return StatusCode(500);
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
            var accessToken = _jwtTokenService.CreateToken(user, userRoles);
            var refreshToken = _jwtTokenService.CreateRefeshToken();
            user.RefreshToken = refreshToken;
            user.DateExpireRefreshToken = DateTime.Now.AddDays(7);
            _userService.Update(user);
            var result = await _userService.SaveChangeAsync();
            if (result)
            {
                return Ok(new { token = accessToken, refreshToken });

            }
            return BadRequest("Failed to update user's token");
        }

        [HttpDelete("signOut")]
        public async Task<IActionResult> SignOut()
        {
            var user = await _currentUserService.GetUser();
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
            var user = await _userService.FindAsync(userId);
            if (user == null || !(user.Status != 0) || user.RefreshToken != refreshToken || user.DateExpireRefreshToken < DateTime.UtcNow)
            {
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
            var token = _jwtTokenService.CreateToken(user, userRoles);
            _userService.Update(user);
            await _userService.SaveChangeAsync();
            return Ok(new { token = token, refreshToken = newRefreshToken });
        }
    }
}
