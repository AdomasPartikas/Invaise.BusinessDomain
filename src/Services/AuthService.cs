using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Utils;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;
using AutoMapper;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling user authentication and authorization.
/// </summary>
public class AuthService(IDatabaseService dbService, IConfiguration configuration, IMapper mapper, IEmailService emailService) : IAuthService
{    
    // Secret salt for email hashing - this would ideally be in app settings

    readonly string emailSalt = configuration["JWT:EmailSalt"] ?? throw new InvalidOperationException("Email salt not configured");

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterModel registration)
    {
        // Hash the email for storage and lookup
        string hashedEmail = HashEmail(registration.Email);
        
        // Check if user with this email hash already exists
        var existingUser = await dbService.GetUserByEmailAsync(hashedEmail);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }
        
        // Create new user with hashed email and password
        var user = new User
        {
            DisplayName = registration.Name,
            Email = hashedEmail, // Store hashed email
            PasswordHash = HashPassword(registration.Password),
            Role = "User", // Default role for new users
            EmailVerified = false
        };
        
        // Save to database
        var createdUser = await dbService.CreateUserAsync(user);
        
        // Generate JWT token
        var (token, expiresAt) = GenerateJwtToken(createdUser);
        
        // Map user to DTO - use original email for DTO
        var userDto = mapper.Map<UserDto>(user);
        userDto.Email = registration.Email; // Use original email for UI display

        try
        {
            // Send registration confirmation email
            await emailService.SendRegistrationConfirmationEmailAsync(registration.Email, registration.Name);
        }
        catch (Exception ex)
        {
            // Log error but continue - we don't want to fail registration if email fails
            System.Console.WriteLine(ex.Message);
        }
        
        // Return authentication response
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = userDto
        };
    }
    
    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginModel login)
    {
        // Hash the provided email for lookup
        string hashedEmail = HashEmail(login.Email);
        
        // Find user by hashed email
        var user = await dbService.GetUserByEmailAsync(hashedEmail);

        if (user == null)
        {
            throw new InvalidOperationException("Invalid email or password");
        }
        
        // Check if user account is active
        if (!user.IsActive)
        {
            throw new InvalidOperationException("This account is inactive");
        }
        
        // Validate password
        if (!ValidatePassword(login.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password");
        }
        
        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow.ToLocalTime();
        await dbService.UpdateUserAsync(user);
        
        // Generate JWT token
        var (token, expiresAt) = GenerateJwtToken(user);
        
        // Map user to DTO - use original email for DTO
        var userDto = mapper.Map<UserDto>(user);
        userDto.Email = login.Email; // Use original email for UI display
        
        // Return authentication response
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = userDto
        };
    }

    public async Task<AuthResponse> ServiceLoginAsync(ServiceLoginModel model)
    {
        // Validate the request
        if (string.IsNullOrEmpty(model.Id) || string.IsNullOrEmpty(model.Key))
            throw new InvalidOperationException("Invalid service account credentials");

        // Get the service account
        var serviceAccount = await dbService.GetServiceAccountAsync(model.Id);

        if (serviceAccount == null)
            throw new InvalidOperationException("Service account not found");

        // Validate the key
        if (!ValidatePassword(model.Key, serviceAccount.Key))
            throw new InvalidOperationException("Invalid service account credentials");
        
        // Generate JWT token
        var (token, expiresAt) = GenerateJwtToken(serviceAccount);
        
        // Map as UserDto
        var serviceDto = mapper.Map<UserDto>(serviceAccount);
        
        // Return authentication response
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = serviceDto
        };
    }

    public async Task<AuthResponse> RefreshToken(RefreshModel model)
    {
        // Validate the request
        if (string.IsNullOrEmpty(model.Token))
            throw new InvalidOperationException("Invalid token");

        // Validate the token and get user ID
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(model.Token);
        var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid")?.Value;

        if (userId == null)
            throw new InvalidOperationException("Invalid token");

        // Get the user from the database
        var user = await dbService.GetUserByIdAsync(userId);

        if (user == null)
            throw new InvalidOperationException("User not found");

        // Generate a new JWT token
        var (token, expiresAt) = GenerateJwtToken(user);
        
        // Map user to DTO
        var userDto = mapper.Map<UserDto>(user);
        
        // Return authentication response
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = userDto
        };
    }

    public async Task<ServiceAccountDto> ServiceRegisterAsync(string name, string[] permissions)
    {        
        // Create new service account
        var serviceAccountDto = new ServiceAccountDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            KeyUnhashed = GenerateSecureKey(),
            Role = "Service",
            Permissions = permissions
        };

        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountDto.Id,
            Name = serviceAccountDto.Name,
            Key = HashPassword(serviceAccountDto.KeyUnhashed),
            Role = serviceAccountDto.Role,
            Permissions = serviceAccountDto.Permissions
        };
        
        // Save to database
        await dbService.CreateServiceAccountAsync(serviceAccount);
        
        return serviceAccountDto;
    }
    
    /// <inheritdoc />
    public (string token, DateTime expiresAt) GenerateJwtToken(User user)
    {
        var jwtKey = configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key not configured");
        var jwtIssuer = configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer not configured");
        var jwtAudience = configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience not configured");
        var jwtExpiryMinutes = int.Parse(configuration["JWT:ExpiryInMinutes"] ?? "30");
        
        var key = Encoding.ASCII.GetBytes(jwtKey);
        var tokenHandler = new JwtSecurityTokenHandler();
        var expiresAt = DateTime.UtcNow.ToLocalTime().AddMinutes(jwtExpiryMinutes);
        
        // Note: The Email claim contains the hashed email
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = expiresAt,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    public (string token, DateTime expiresAt) GenerateJwtToken(ServiceAccount serviceAccount)
    {
        var jwtKey = configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key not configured");
        var jwtIssuer = configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer not configured");
        var jwtAudience = configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience not configured");
        var jwtExpiryMinutes = int.Parse(configuration["JWT:ExpiryInMinutes"] ?? "30");
        
        var key = Encoding.ASCII.GetBytes(jwtKey);
        var tokenHandler = new JwtSecurityTokenHandler();
        var expiresAt = DateTime.UtcNow.ToLocalTime().AddMinutes(jwtExpiryMinutes);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, serviceAccount.Id),
                new Claim(ClaimTypes.Role, serviceAccount.Role)
            }),
            Expires = expiresAt,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        foreach (var permission in serviceAccount.Permissions)
        {
            tokenDescriptor.Subject.AddClaim(new Claim("permission", permission));
        }
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }
    
    /// <inheritdoc />
    public bool ValidatePassword(string password, string passwordHash)
    {
        return BC.Verify(password, passwordHash);
    }
    
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BC.HashPassword(password);
    }

    /// <inheritdoc />
    public string HashEmail(string email)
    {        
        // Normalize the email
        string normalizedEmail = email.ToLower().Trim();
        
        // Create a deterministic hash using SHA-256
        using (var sha256 = SHA256.Create())
        {
            // Combine email with salt
            byte[] emailBytes = Encoding.UTF8.GetBytes(normalizedEmail + emailSalt);
            byte[] hashBytes = sha256.ComputeHash(emailBytes);
            
            // Convert to hex string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }
            
            return builder.ToString();
        }
    }

    public string GenerateSecureKey()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc />
    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            // Hash the email for database lookup
            string hashedEmail = HashEmail(email);
            
            // Find user by hashed email
            var user = await dbService.GetUserByEmailAsync(hashedEmail);

            if (user == null)
            {
                // User not found, but don't reveal this information
                // For security reasons, we'll return true anyway
                return true;
            }
            
            // Generate a temporary password (using GUID for randomness)
            string temporaryPassword = Guid.NewGuid().ToString("N").Substring(0, 12);
            
            // Update user's password with the hashed temporary password
            user.PasswordHash = HashPassword(temporaryPassword);
            
            // Save the updated user to the database
            await dbService.UpdateUserAsync(user);
            
            // Send email with the temporary password
            await emailService.SendPasswordResetEmailAsync(email, user.DisplayName, temporaryPassword);
            
            return true;
        }
        catch (Exception ex)
        {
            // Log the error
            System.Console.WriteLine($"Error in ForgotPasswordAsync: {ex.Message}");
            return false;
        }
    }
} 