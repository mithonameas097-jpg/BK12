using _bk12.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
namespace _bk12.Areas.Identity.Pages.Account
{
    public class ConfirmLoginPromptModel : PageModel
    {
        private readonly SignInManager<_bk12.Data.ApplicationUser> _signInManager;
        private readonly ILogger<ConfirmLoginPromptModel> _logger;
        public ConfirmLoginPromptModel(SignInManager<_bk12.Data.ApplicationUser> signInManager, ILogger<ConfirmLoginPromptModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }
        [BindProperty(SupportsGet = true)]
        public string Email { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string LoginProvider { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string ProviderKey { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }
        public async Task<IActionResult> OnPostConfirmAsync()
        {
            ReturnUrl = ReturnUrl ?? Url.Content("~/");
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl });
            }
            var result = await _signInManager.ExternalLoginSignInAsync(LoginProvider, ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                var pictureUrl = info.Principal.FindFirstValue("urn:google:picture")
                         ?? info.Principal.FindFirstValue("picture");
                if (!string.IsNullOrEmpty(pictureUrl))
                {
                    var user = await _signInManager.UserManager.FindByLoginAsync(LoginProvider, ProviderKey);
                    if (user != null && user.ProfilePictureUrl != pictureUrl)
                    {
                        user.ProfilePictureUrl = pictureUrl;
                        await _signInManager.UserManager.UpdateAsync(user);
                    }
                }
                _logger.LogInformation("User confirmed login with {LoginProvider}.", LoginProvider);
                return LocalRedirect(ReturnUrl);
            }

            ErrorMessage = "Error signing in with external provider.";
            return RedirectToPage("./Login", new { ReturnUrl });
        }
        public IActionResult OnPostCancel()
        {
            return RedirectToPage("./Login", new { ReturnUrl });
        }
        [TempData]
        public string? ErrorMessage { get; set; }
    }
}
