using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Represents a service for handling authentication and authorization.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the provided registration data.
    /// </summary>
    /// <param name="registration">The registration data for the new user.</param>
    /// <returns>The authentication response containing token and user information.</returns>
    Task<AuthResponse> RegisterAsync(RegisterModel registration);
    
    /// <summary>
    /// Authenticates a user with the provided login credentials.
    /// </summary>
    /// <param name="login">The login credentials.</param>
    /// <returns>The authentication response containing token and user information.</returns>
    Task<AuthResponse> LoginAsync(LoginModel login);

    /// <summary>
    /// Refreshes the authentication token using the provided refresh model.
    /// </summary>
    /// <param name="model">The refresh model containing the necessary data to refresh the token.</param>
    /// <returns>The authentication response containing the new token and user information.</returns>
    Task<AuthResponse> RefreshToken(RefreshModel model);

    /// <summary>
    /// Authenticates a service account using the provided login model.
    /// </summary>
    /// <param name="model">The login model containing service account credentials.</param>
    /// <returns>The authentication response containing token and service account information.</returns>
    Task<AuthResponse> ServiceLoginAsync(ServiceLoginModel model);

    /// <summary>
    /// Registers a new service account with the specified name and permissions.
    /// </summary>
    /// <param name="name">The name of the service account.</param>
    /// <param name="permissions">The permissions assigned to the service account.</param>
    /// <returns>The service account data transfer object containing account details.</returns>
    Task<ServiceAccountDto> ServiceRegisterAsync(string name, string[] permissions);
    
    /// <summary>
    /// Generates a JWT token for a user.
    /// </summary>
    /// <param name="user">The user to generate a token for.</param>
    /// <returns>The generated token and its expiration date.</returns>
    (string token, DateTime expiresAt) GenerateJwtToken(User user);
    
    /// <summary>
    /// Validates a password against its hash.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="passwordHash">The password hash to validate against.</param>
    /// <returns>True if the password is valid, false otherwise.</returns>
    bool ValidatePassword(string password, string passwordHash);
    
    /// <summary>
    /// Hashes a password using a secure algorithm.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Hashes an email using a secure algorithm.
    /// </summary>
    /// <param name="email">The email to hash.</param>
    /// <returns>The hashed email.</returns>
    string HashEmail(string email);

    /// <summary>
    /// Generates a secure key for cryptographic operations.
    /// </summary>
    /// <returns>A securely generated key as a string.</returns>
    string GenerateSecureKey();

    /// <summary>
    /// Handles a forgot password request by generating a temporary password and sending it via email.
    /// </summary>
    /// <param name="email">The email of the user requesting a password reset.</param>
    /// <returns>True if the password was reset successfully, false otherwise.</returns>
    Task<bool> ForgotPasswordAsync(string email);
} 