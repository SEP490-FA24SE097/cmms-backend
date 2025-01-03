﻿
using CMMS.API.OptionsSetup;
using CMMS.API.Services;
using CMMS.API.Services.BackgroundJob;
using CMMS.Core.Entities;
using CMMS.Infrastructure;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Handlers;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
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
using System.Text.Json.Serialization;
using CMMS.Infrastructure.BackgroundService;
using CMMS.Infrastructure.SignalRHub;
using DinkToPdf.Contracts;
using DinkToPdf;
using CMMS.Infrastructure.InvoicePdf;
using Microsoft.Extensions.FileProviders;
using System;

namespace CMMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var env = builder.Environment;
            // Add CORS services
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin() // Cho phép tất cả các origin
                               .AllowAnyMethod() // Cho phép tất cả các phương thức (GET, POST, PUT, DELETE, ...)
                               .AllowAnyHeader(); // Cho phép tất cả các header
                    });
            });
            // background worker 
            //builder.Services.AddHostedService<PaymentBackgroundService>();

            builder.Services.AddSignalR();
            builder.Services.AddHostedService<LowStockNotificationService>();
            builder.Services.AddHostedService<NewRequestNotificationService>();
            builder.Services.AddScoped<LowStockNotificationService>();
            builder.Services.AddScoped<NewRequestNotificationService>();
            builder.Services.AddHttpClient();
            builder.Services.AddDistributedMemoryCache();

            // Add services to the container.
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().AddSignInManager()
            .AddTokenProvider(TokenOptions.DefaultProvider, typeof(DataProtectorTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultEmailProvider, typeof(EmailTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultPhoneProvider, typeof(PhoneNumberTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultAuthenticatorProvider, typeof(AuthenticatorTokenProvider<ApplicationUser>));



            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            })
            .AddCookie()
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
                options.SignInScheme = "Identity.External";

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

            builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
            builder.Services.AddSingleton<IMailService, MailService>();
            builder.Services.AddScoped<ITransaction, EfTransaction>();
            builder.Services.AddScoped<IGenerateInvoicePdf, GenerateInvoicePdf>();


            // Configure JSON options globally
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.MaxDepth = 64;
                options.JsonSerializerOptions.WriteIndented = true; // Optional for pretty printing
            });

            // swagger options
            builder.Services.AddSwaggerGen(option =>
            {
                SwaggerConfigOptionsSetup.SwaggerConfigOptions(option);
            });

            // auto mapper
            builder.Services.AddAutoMapper(typeof(Program));
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase.json")
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseCors("AllowAllOrigins");
            app.MapHub<StoreNotificationHub>("/store-notification-hub");
            app.MapHub<WarehouseNotificationHub>("/warehouse-notification-hub");
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath, "Exports", "Invoices")),
                RequestPath = "/Files"
            });

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
