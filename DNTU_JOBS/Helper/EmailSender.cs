using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DNTU_JOBS.Helper
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var smtpClient = new SmtpClient
                {
                    Host = _configuration["Smtp:Host"],
                    Port = int.Parse(_configuration["Smtp:Port"]),
                    Credentials = new System.Net.NetworkCredential(
                        _configuration["Smtp:Username"],
                        _configuration["Smtp:Password"]
                    ),
                    EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true")
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Smtp:FromEmail"], _configuration["Smtp:DisplayName"]),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);

                // Gửi email
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {email} with subject {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {email}. Exception: {ex.Message}");
                throw; // Có thể ném lỗi lại nếu cần xử lý ở tầng cao hơn
            }
        }
    }
}
