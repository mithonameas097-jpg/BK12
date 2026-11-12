using _bk12.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace _bk12.Services
{
    public class RoleSessionService
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;

        public RoleSessionService(IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
        {
            _claimsFactory = claimsFactory;
        }

        public async Task SignInWithRoleAsync(HttpContext httpContext, ApplicationUser user, string activeRole, bool isPersistent)
        {
            var principal = await _claimsFactory.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity!;
            var existingRoles = identity.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
            foreach (var claim in existingRoles)
            {
                identity.RemoveClaim(claim);
            }

            identity.AddClaim(new Claim(ClaimTypes.Role, activeRole));
            identity.AddClaim(new Claim("ActiveRole", activeRole));
            await httpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties { IsPersistent = isPersistent });
        }
    }
}