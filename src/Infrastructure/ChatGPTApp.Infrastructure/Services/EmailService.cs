using ChatGPTApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace ChatGPTApp.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_config["App:BaseUrl"]}/reset-password?token={resetToken}&email={Uri.EscapeDataString(toEmail)}";

        var body = $@"
            <h2>Password Reset Request</h2>
            <p>Click the button below to reset your password. This link expires in 2 hours.</p>
            <a href='{resetUrl}' style='background:#6366f1;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;'>
                Reset Password
            </a>
            <p>If you did not request this, please ignore this email.</p>
        ";

        await SendEmailAsync(toEmail, "Reset Your Password", body, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName, CancellationToken cancellationToken = default)
    {
        var body = $@"
            <h2>Welcome, {firstName}!</h2>
            <p>Your account has been created successfully.</p>
            <p>You can now log in and start using ChatGPT App.</p>
        ";

        await SendEmailAsync(toEmail, "Welcome to ChatGPT App", body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:Username"] ?? "";
            var smtpPass = _config["Email:Password"] ?? "";
            var fromEmail = _config["Email:From"] ?? smtpUser;
            var fromName = _config["Email:FromName"] ?? "ChatGPT App";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // Don't throw - email failure shouldn't break the flow
        }
    }
}
