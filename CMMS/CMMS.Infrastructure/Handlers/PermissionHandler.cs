using CMMS.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;

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
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            PermissionRequirment requirement)
        {
            throw new NotImplementedException();
        }
    }
}
