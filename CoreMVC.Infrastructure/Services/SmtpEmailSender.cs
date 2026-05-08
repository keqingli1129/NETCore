using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreMVC.Infrastructure.Services
{
    // Minimal SMTP email sender moved from the Web project into Infrastructure to keep concrete
    // implementations in the Infrastructure layer. Implements both the application-level
    // IEmailSender and the Identity UI IEmailSender to be usable by Identity pages.
    public class SmtpEmailSender : CoreMVC.Application.Interfaces.IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(logger);
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(subject)) subject = string.Empty;

            var host = _configuration["Email:Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("SMTP host is not configured (Email:Smtp:Host). Skipping send.");
                return;
            }

            var port = 25;
            if (int.TryParse(_configuration["Email:Smtp:Port"], out var p)) port = p;

            var enableSsl = false;
            if (bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var es)) enableSsl = es;

            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];
            var from = _configuration["Email:From"] ?? username ?? "noreply@localhost";

            using var message = new MailMessage(from, email, subject, htmlMessage) { IsBodyHtml = true };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            try
            {
                await client.SendMailAsync(message).ConfigureAwait(false);
                _logger.LogInformation("Sent email to {Email} via {Host}:{Port}", email, host, port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                throw;
            }
        }
    }
}
