
using CMMS.API.OptionsSetup;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Infrastructure;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;

namespace CMMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // jwt options
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();
            builder.Services.ConfigureOptions<JwtBearerOptionSetup>();


            // authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddCustomAuthorizationPolicies();
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
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
