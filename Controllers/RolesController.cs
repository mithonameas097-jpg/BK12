using _bk12.Data;
using _bk12.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace _bk12.Controllers
{
    [Authorize]
    [Route("roles")]
    public class RolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleService _roleService;
        private readonly RoleSessionService _roleSessionService;

        public RolesController(
            UserManager<ApplicationUser> userManager,
            RoleService roleService,
            RoleSessionService roleSessionService)
        {
            _userManager = userManager;
            _roleService = roleService;
            _roleSessionService = roleSessionService;
        }

        [HttpPost("switch")]
        [ValidateAntiForgeryToken]     
        public async Task<IActionResult> Switch(string role)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var roles = await _roleService.GetRolesAsync(user);
            if (!roles.Contains(role))
                return Forbid(); 

            await _roleSessionService.SignInWithRoleAsync(HttpContext, user, role, isPersistent: true);
            
            return RedirectToAction("Index", "Home");
        }
    }
}