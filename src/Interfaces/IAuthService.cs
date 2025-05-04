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

    Task<AuthResponse> RefreshToken(RefreshModel model);

    Task<AuthResponse> ServiceLoginAsync(ServiceLoginModel model);
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

    string GenerateSecureKey();
} 