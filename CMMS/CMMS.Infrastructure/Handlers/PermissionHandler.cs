using CMMS.Infrastructure.Constant;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security;

namespace CMMS.Infrastructure.Handlers
{
    // create HasPermissionAttribute 
    public sealed class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(params Permission[] permissions)
            : base(policy: string.Join(",", permissions.Select(p => p.ToString())))
        {

        }
    }

    public class PermissionRequirment : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirment(string permission)
        {
            Permission = permission;
        }

    }
    public class PermissionHandler : AuthorizationHandler<PermissionRequirment>
    {

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PermissionHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            PermissionRequirment requirement)
        {
            string? userId = context.User.Claims
               .FirstOrDefault(c => c.Type == CustomClaims.UserId)?.Value;

            if (userId is null)
            {
                context.Fail();
                return;
            }

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IPermissionSerivce permissionService = scope.ServiceProvider.GetRequiredService<IPermissionSerivce>();

            var permissions = context.User.Claims
               .FirstOrDefault(c => c.Type == CustomClaims.Permissions)?.Value;

            if (permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            } else
            {
                context.Fail();
            }
        }
    }
}

