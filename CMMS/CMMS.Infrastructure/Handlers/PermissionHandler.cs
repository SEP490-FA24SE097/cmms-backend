using CMMS.Infrastructure.Enums;
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
            string userId = context.User.Claims
                .FirstOrDefault(_ => _.Type == JwtRegisteredClaimNames.Sid)?.Value;

            if (!Guid.TryParse(userId, out Guid parsedUserId)) {
                return;
            }

            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            
        }
    }
}

