using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace _bk12.Services
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
    string recipient,
    string subject,
    string message)
        {
            var host = _configuration["Authentication:Email:Host"];
            var port = _configuration["Authentication:Email:Port"];
            var username = _configuration["Authentication:Email:Username"];
            var password = _configuration["Authentication:Email:Password"];

            if (string.IsNullOrEmpty(host))
                throw new Exception("Email:Host is NULL");

            if (string.IsNullOrEmpty(port))
                throw new Exception("Email:Port is NULL");

            if (string.IsNullOrEmpty(username))
                throw new Exception("Email:Username is NULL");

            if (string.IsNullOrEmpty(password))
                throw new Exception("Email:Password is NULL");

            if (string.IsNullOrEmpty(recipient))
                throw new Exception("Recipient is NULL");

            if (string.IsNullOrEmpty(subject))
                throw new Exception("Subject is NULL");

            if (string.IsNullOrEmpty(message))
                throw new Exception("Message is NULL");

            var email = new MimeMessage();

            email.From.Add(MailboxAddress.Parse(username));
            email.To.Add(MailboxAddress.Parse(recipient));

            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = message
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                host,
                int.Parse(port),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                username,
                password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
    }
}