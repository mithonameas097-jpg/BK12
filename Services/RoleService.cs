using _bk12.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace _bk12.Services
{
    public class RoleService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<string?> GetDefaultRoleAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return "Admin";
            if (roles.Contains("ClubOwner"))
                return "ClubOwner";
            if (roles.Contains("Staff"))
                return "Staff";
            if (roles.Contains("User"))
                return "User";
            return null;
        }
    }
}