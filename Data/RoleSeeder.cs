using Microsoft.AspNetCore.Identity;

namespace _bk12.Data
{
    public static class RoleSeeder
    {
        public static async Task RoleSeederAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            string[] Roles =
            {
                "User",
                "Staff",
                "ClubOwner",
                "Admin"
            };
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }
        }
    }
}
