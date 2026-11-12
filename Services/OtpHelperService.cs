using _bk12.Data;
using _bk12.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _bk12.Services;

public class OtpHelperService
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<OtpHelperService> _logger;

    public OtpHelperService(
        ApplicationDbContext context,
        EmailService emailService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<OtpHelperService> logger)
    {
        _context = context;
        _emailService = emailService;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }
    public async Task GenerateAndSendOtpAsync(Guid userId, string email, string otpPurpose)
    {
        var otp = Random.Shared.Next(100000, 1000000).ToString();
        var existingOtp = await _context.Email_OTPs.FirstOrDefaultAsync(x => x.User_ID == userId);
        if (existingOtp != null)
        {
            existingOtp.OTP_Code = otp;
            existingOtp.Expiry_Date = DateTime.Now.AddMinutes(3);
            existingOtp.Is_Used = false;
            existingOtp.Created_Date = DateTime.Now;
            existingOtp.Otp_purpose = otpPurpose;
            _context.Email_OTPs.Update(existingOtp);
        }
        else
        {
            var emailOtp = new EmailOtp
            {
                User_ID = userId,
                OTP_Code = otp,
                Expiry_Date = DateTime.Now.AddMinutes(3),
                Is_Used = false,
                Created_Date = DateTime.Now,
                Otp_purpose = otpPurpose
            };
            await _context.Email_OTPs.AddAsync(emailOtp);
        }

        await _context.SaveChangesAsync();

        var templatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Models", "LoginOtpEmail.html");
        string emailBody;

        if (File.Exists(templatePath))
        {
            emailBody = await File.ReadAllTextAsync(templatePath);
            emailBody = emailBody.Replace("{otp}", otp);
        }
        else
        {
            _logger.LogError("LoginOtpEmail.html not found at path: {Path}.", templatePath);
            emailBody = $"<p>Your BK12 Verification Code is: <strong>{otp}</strong>. Expires in 3 minutes.</p>";
        }

        await _emailService.SendEmailAsync(
            email,
            "BK12 Verification Code",
            emailBody
        );
    }
}