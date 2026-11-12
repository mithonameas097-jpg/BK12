using _bk12.Data;
using _bk12.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bk12.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LinkAccountPromptModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<LinkAccountPromptModel> _logger;

        public LinkAccountPromptModel(
            ApplicationDbContext context,
            EmailService emailService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<LinkAccountPromptModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserIdStr { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? LoginProvider { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ProviderKey { get; set; }

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(UserIdStr))
            {
                _logger.LogWarning("Parameters missing on GET for LinkAccountPrompt.");
                return RedirectToPage("./Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLinkAsync()
        {
            _logger.LogInformation("OnPostLinkAsync called. Email: '{Email}', UserIdStr: '{UserIdStr}'", Email, UserIdStr);

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(UserIdStr) || !Guid.TryParse(UserIdStr, out Guid userId))
            {
                _logger.LogWarning("Validation failed during POST. Email or UserId is invalid.");
                return RedirectToPage("./Login");
            }

            var otp = Random.Shared.Next(100000, 1000000).ToString();
            var otpRecord = new _bk12.Models.EmailOtp
            {
                User_ID = userId,
                OTP_Code = otp,
                Expiry_Date = DateTime.Now.AddMinutes(5),
                Is_Used = false,
                Created_Date = DateTime.Now,
                Otp_purpose = "LinkGoogle"
            };

            _context.Email_OTPs.Add(otpRecord);
            await _context.SaveChangesAsync();

            var templatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Models", "LoginOtpEmail.html");
            string emailBody;
            if (System.IO.File.Exists(templatePath))
            {
                emailBody = await System.IO.File.ReadAllTextAsync(templatePath);
                emailBody = emailBody.Replace("{otp}", otp);
            }
            else
            {
                emailBody = $"<p>Your BK12 Verification Code is: <strong>{otp}</strong>. Expires in 5 minutes.</p>";
            }

            await _emailService.SendEmailAsync(
                Email,
                "BK12 Verification Code",
                emailBody
            );

            TempData["GoogleLoginProvider"] = LoginProvider;
            TempData["GoogleProviderKey"] = ProviderKey;
            TempData["GoogleEmail"] = Email;
            TempData["GoogleReturnUrl"] = ReturnUrl ?? "~/";
            TempData["ExistingUserId"] = UserIdStr;

            return RedirectToAction(
                "VerifyOtp",
                "Account",
                new
                {
                    userId = userId,
                    purpose = "LinkGoogle",
                    returnUrl = ReturnUrl ?? "~/"
                });
        }
    }
}