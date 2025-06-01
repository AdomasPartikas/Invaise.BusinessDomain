namespace Invaise.BusinessDomain.API.Interfaces
{
    /// <summary>
    /// Interface for email service operations including user notifications and password resets
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a welcome email to newly registered users confirming their account creation
        /// </summary>
        /// <param name="to">The recipient's email address</param>
        /// <param name="username">The username of the newly registered user</param>
        /// <returns>A task that represents the asynchronous email sending operation</returns>
        Task SendRegistrationConfirmationEmailAsync(string to, string username);
        
        /// <summary>
        /// Sends a password reset email containing a temporary password to the user
        /// </summary>
        /// <param name="to">The recipient's email address</param>
        /// <param name="username">The username requesting the password reset</param>
        /// <param name="temporaryPassword">The temporary password generated for the user</param>
        /// <returns>A task that represents the asynchronous email sending operation</returns>
        Task SendPasswordResetEmailAsync(string to, string username, string temporaryPassword);
    }
} 