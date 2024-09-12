using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CMMS.API.OptionsSetup
{
    public class JwtBearerOptionSetup : IConfigureOptions<JwtBearerOptions>
    {
        private IConfiguration _configuration;

        public JwtBearerOptionSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Configure(JwtBearerOptions options)
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAudience = _configuration.GetValue<String>("JWT:ValidAudience"),
                ValidIssuer = _configuration.GetValue<String>("JWT:ValidIssuer"),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<String>("JWT:Secret"))),
            };
        }
    }
}
