using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;
using AutoMapper;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling user authentication and authorization.
/// </summary>
public class AuthService(IDatabaseService dbService, IConfiguration configuration, IMapper mapper) : IAuthService
{    
    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterModel registration)
    {
        // Check if user already exists
        var existingUser = await dbService.GetUserByEmailAsync(registration.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }
        
        // Create new user
        var user = new User
        {
            DisplayName = registration.Name,
            Email = registration.Email,
            PasswordHash = HashPassword(registration.Password),
            Role = "User", // Default role for new users
            EmailVerified = false
        };
        
        // Save to database
        var createdUser = await dbService.CreateUserAsync(user);
        
        // Generate JWT token
        var (token, expiresAt) = GenerateJwtToken(createdUser);
        
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
    
    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginModel login)
    {
        // Find user by email
        var user = await dbService.GetUserByEmailAsync(login.Email);

        if (user == null)
        {
            throw new InvalidOperationException("Invalid email or password");
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
        var jwtExpiryHours = int.Parse(configuration["JWT:ExpiryInHours"] ?? "24");
        
        var key = Encoding.ASCII.GetBytes(jwtKey);
        var tokenHandler = new JwtSecurityTokenHandler();
        var expiresAt = DateTime.UtcNow.ToLocalTime().AddHours(jwtExpiryHours);
        
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

    public string GenerateSecureKey()
    {
        return Guid.NewGuid().ToString("N");
    }
} 