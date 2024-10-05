
using CMMS.API.OptionsSetup;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Infrastructure;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Encodings.Web;

namespace CMMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add CORS services
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:5001", "")  
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();  // Cho phép gửi cookie
                    });
            });


            //builder.Services.AddDistributedMemoryCache(); // Or use a distributed cache for production.


            // Add services to the container.
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().AddSignInManager()
            .AddTokenProvider(TokenOptions.DefaultProvider, typeof(DataProtectorTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultEmailProvider, typeof(EmailTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultPhoneProvider, typeof(PhoneNumberTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultAuthenticatorProvider, typeof(AuthenticatorTokenProvider<ApplicationUser>));
            
            
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // timeout time
                options.SlidingExpiration = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });


            //Cookie Policy needed for External Auth
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.Secure = CookieSecurePolicy.Always;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            // jwt options
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
                };
            })// google options
            .AddGoogle(options =>
            {
                var clientId = builder.Configuration["Authentication:Google:ClientId"];
                var clientSecert = builder.Configuration["Authentication:Google:ClientSecret"];
                options.ClientId = clientId;
                options.ClientSecret = clientSecert;
                options.CallbackPath = "/api/ExternalLogin/google-response";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.StateDataFormat = new SecureDataFormat<AuthenticationProperties>(new PropertiesSerializer(),
                    DataProtectionProvider.Create("ASP.NET Core").CreateProtector("OAuth.State"));
            });




            // authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddCustomAuthorizationPolicies();
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            });


            // DI 
            builder.Services.AddInfrastructureServices(builder.Configuration);

            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();


            // swagger options
            builder.Services.AddSwaggerGen(option =>
            {
                SwaggerConfigOptionsSetup.SwaggerConfigOptions(option);
            });

            // auto mapper
            builder.Services.AddAutoMapper(typeof(Program));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseCors("AllowAll");

            //app.UseSwagger();
            //app.UseSwaggerUI();

            app.UseRouting();

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseCookiePolicy();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
