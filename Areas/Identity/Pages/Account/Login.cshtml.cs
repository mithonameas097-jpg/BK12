// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using _bk12.Data;
using _bk12.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace _bk12.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<_bk12.Data.ApplicationUser> _signInManager;
    private readonly UserManager<_bk12.Data.ApplicationUser> _userManager;
    private readonly EmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoginModel> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly OtpHelperService _otpHelperService;

    public LoginModel(
    SignInManager<_bk12.Data.ApplicationUser> signInManager,
    UserManager<_bk12.Data.ApplicationUser> userManager,
    ILogger<LoginModel> logger,
    EmailService emailService,
    ApplicationDbContext context,
    IWebHostEnvironment webHostEnvironment,
    OtpHelperService otpHelperService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _emailService = emailService;
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _otpHelperService = otpHelperService; 
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        ExternalLogins =
            (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid login attempt.");

                return Page();
            }

            var passwordValid = await _userManager.CheckPasswordAsync(
                user,
                Input.Password);

            if (passwordValid)
            {
                await _otpHelperService.GenerateAndSendOtpAsync(user.Id,user.Email,"Login");

                return RedirectToAction(
                    "VerifyOtp",
                    "Account",
                    new
                    {
                        userId = user.Id,
                        purpose = "Login",
                        returnUrl = returnUrl,
                        rememberMe = Input.RememberMe
                    });
            }
            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");
        }

        return Page();
    }
}
