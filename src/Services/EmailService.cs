using Invaise.BusinessDomain.API.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Invaise.BusinessDomain.API.Services
{
    public class EmailService(IConfiguration configuration, Serilog.ILogger logger) : IEmailService
    {

        private readonly string server = configuration["Email:SmtpServer"] ?? throw new ArgumentNullException("SMTP server is not configured.");
        private readonly int port = int.Parse(configuration["Email:Port"] ?? throw new ArgumentNullException("SMTP port is not configured."));
        private readonly bool useSsl = bool.Parse(configuration["Email:UseSsl"] ?? throw new ArgumentNullException("SMTP SSL setting is not configured."));
        private readonly string username = configuration["Email:Username"] ?? throw new ArgumentNullException("SMTP username is not configured.");
        private readonly string password = configuration["Email:Password"] ?? throw new ArgumentNullException("SMTP password is not configured.");
        private readonly string senderEmail = configuration["Email:SenderEmail"] ?? throw new ArgumentNullException("Sender email is not configured.");
        private readonly string senderName = configuration["Email:SenderName"] ?? throw new ArgumentNullException("Sender name is not configured.");
        private readonly bool useAuthentication = bool.Parse(configuration["Email:UseAuthentication"] ?? throw new ArgumentNullException("SMTP authentication setting is not configured."));

        public async Task SendRegistrationConfirmationEmailAsync(string to, string username)
        {
            var subject = "Welcome to Invaise!";
            var body = $@"
                <h1>Welcome to Invaise, {username}!</h1>
                <p>Thank you for registering with us. Your account has been successfully created.</p>
                <p>You can now log in using your credentials.</p>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string to, string username, string temporaryPassword)
        {
            var subject = "Invaise - Password Reset";
            var body = $@"
                <h1>Password Reset</h1>
                <p>Hello {username},</p>
                <p>We received a request to reset your password.</p>
                <p>Your temporary password is: <strong>{temporaryPassword}</strong></p>
                <p>Please log in with this temporary password and change it immediately for security reasons.</p>
                <p>If you didn't request this password reset, please contact support immediately.</p>
            ";

            await SendEmailAsync(to, subject, body);
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    server,
                    port,
                    useSsl
                );

                if (useAuthentication)
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                
                logger.Debug($"Registration confirmation email sent to {to}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error sending registration confirmation email to {to}");
                throw;
            }
        }
    }
} 