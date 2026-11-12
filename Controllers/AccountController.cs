using _bk12.Data;
using _bk12.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace _bk12.Controllers
{
    public class AccountController : Controller
    {
        private readonly Connection _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleSessionService _roleSessionService;

        public AccountController(
            Connection db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleSessionService roleSessionService)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleSessionService = roleSessionService;
        }

        [HttpGet]
        public IActionResult VerifyOtp(Guid userId, string purpose)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(
            Guid userId,
            string purpose,
            bool rememberMe,
            string otp1, string otp2, string otp3, string otp4, string otp5, string otp6)
        {
            var otp = otp1 + otp2 + otp3 + otp4 + otp5 + otp6;

            var table = (DataTable)await _db.ExecuteDB("GET_LATEST_EMAIL_OTP", new SqlParameter[]
            {
                new SqlParameter("@User_ID", userId)
            }, returnTable: true);

            if (table.Rows.Count == 0)
            {
                return Content("OTP not found");
            }

            var row = table.Rows[0];
            bool isUsed = Convert.ToBoolean(row["Is_Used"]);
            DateTime expiryDate = Convert.ToDateTime(row["Expiry_Date"]);
            string storedOtp = row["OTP_Code"] as string ?? string.Empty;

            if (isUsed) return Content("OTP already used!");
            if (DateTime.Now > expiryDate) return Content("OTP expired");
            if (storedOtp != otp) return Content("Invalid OTP");

            await _db.ExecuteDB("UPDATE_OTP_USAGE", new SqlParameter[]
            {
                new SqlParameter("@User_ID", userId),
                new SqlParameter("@OTP_Code", otp)
            });

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Content("User not found");

            if (purpose == "Register")
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            else if (purpose == "LinkGoogle")
            {
                var loginProvider = TempData["GoogleLoginProvider"]?.ToString();
                var providerKey = TempData["GoogleProviderKey"]?.ToString();
                if (!string.IsNullOrEmpty(loginProvider) && !string.IsNullOrEmpty(providerKey))
                {
                    var providerInfo = new UserLoginInfo(loginProvider, providerKey, loginProvider);

                    var addLoginResult = await _userManager.AddLoginAsync(user, providerInfo);
                    if (!addLoginResult.Succeeded)
                    {
                        return Content("Failed to link Google account.");
                    }
                }
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var initialRole = userRoles.FirstOrDefault() ?? "User";
            await _roleSessionService.SignInWithRoleAsync(HttpContext, user, initialRole, isPersistent: rememberMe);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return BadRequest("Role cannot be empty.");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var hasRole = await _userManager.IsInRoleAsync(user, role);
            if (!hasRole)
            {
                return Forbid();
            }
            var authentication = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            var isPersistent = authentication.Properties?.IsPersistent ?? false;
            await _roleSessionService.SignInWithRoleAsync(HttpContext, user, role, isPersistent);

            return Json(new { success = true, activeRole = role });
        }

        [Authorize]
        [HttpGet]
        public IActionResult CurrentRole()
        {
            var activeRole = User.FindFirst("ActiveRole")?.Value;

            return Json(new
            {
                activeRole = activeRole,
                isUser = User.IsInRole("User"),
                isClubOwner = User.IsInRole("ClubOwner"),
                isStaff = User.IsInRole("Staff"),
                isAdmin = User.IsInRole("Admin")
            });
        }
    }
}