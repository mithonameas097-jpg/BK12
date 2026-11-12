using _bk12.Data;
using _bk12.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace _bk12.ViewComponents;

public class RoleSwitcherViewComponent : ViewComponent
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleService _roleService;

    public RoleSwitcherViewComponent(UserManager<ApplicationUser> userManager, RoleService roleService)
    {
        _userManager = userManager;
        _roleService = roleService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Content(string.Empty);

        var roles = await _roleService.GetRolesAsync(user);

        // If the user only has 1 or 0 roles, don't render the switcher at all
        if (roles.Count <= 1) return Content(string.Empty);

        var currentRole = HttpContext.User.FindFirst("ActiveRole")?.Value
                           ?? await _roleService.GetDefaultRoleAsync(user);

        var model = new RoleSwitcherViewModel
        {
            Roles = roles,
            CurrentRole = currentRole
        };

        return View(model);
    }
}

public class RoleSwitcherViewModel
{
    public IList<string> Roles { get; set; } = new List<string>();
    public string CurrentRole { get; set; } = string.Empty;
}