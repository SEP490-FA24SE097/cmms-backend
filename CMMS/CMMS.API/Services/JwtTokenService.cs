using CMMS.Core.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CMMS.API.Services
{
    public interface IJwtTokenService
    {
        //string CreateToken(UserSignInDTO user, List<Claim> roles);
        string CreateToken(ApplicationUser user, IList<String> roles);
        string CreateRefeshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(String? token);
    }
    public class JwtTokenService : IJwtTokenService
    {
        private IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string CreateRefeshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public string CreateToken(ApplicationUser user, IList<String> roles)
        {
            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                }
            }

            var authenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(20),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authenKey, SecurityAlgorithms.HmacSha512Signature)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var validation = new TokenValidationParameters
            {
                ValidateLifetime = false,
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]))
            };

            return new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);

        }
    }
}
