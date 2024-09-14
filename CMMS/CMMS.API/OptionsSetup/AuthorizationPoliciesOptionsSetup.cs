using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace CMMS.API.OptionsSetup
{
    public static class AuthorizationPoliciesOptionsSetup
    {
        public  static void AddCustomAuthorizationPolicies(this AuthorizationOptions options)
        {
            foreach (Permission permission in Enum.GetValues(typeof(Permission)))
            {
                options.AddPolicy(permission.ToString() + "Policy", policy =>
                    policy.Requirements.Add(new PermissionRequirment(permission.ToString())));
            }
        }
    }
}
