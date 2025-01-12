using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace CMMS.API.OptionsSetup
{
    public static class AuthorizationPoliciesOptionsSetup
    {
        public  static void AddCustomAuthorizationPolicies(this AuthorizationOptions options)
        {
            foreach (PermissionName permission in Enum.GetValues(typeof(PermissionName)))
            {
                options.AddPolicy(permission.ToString(), policy =>
                    policy.Requirements.Add(new PermissionRequirment(permission.ToString())));
            }
        }
    }
}
