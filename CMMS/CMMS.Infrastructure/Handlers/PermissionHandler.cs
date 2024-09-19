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
        public HasPermissionAttribute(Permission permission)
            : base(policy: permission.ToString())
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

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return;
            }

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IPermissionSerivce permissionService = scope.ServiceProvider.GetRequiredService<IPermissionSerivce>();

            var permissions = await permissionService.GetUserPermission(userId);
            if(permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            } 
        }
    }
}

