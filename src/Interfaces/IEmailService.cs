namespace Invaise.BusinessDomain.API.Interfaces
{
    public interface IEmailService
    {
        Task SendRegistrationConfirmationEmailAsync(string to, string username);
        Task SendPasswordResetEmailAsync(string to, string username, string temporaryPassword);
    }
} 