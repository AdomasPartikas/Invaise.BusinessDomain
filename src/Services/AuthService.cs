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
public class AuthService : IAuthService
{
    private readonly IDatabaseService _dbService;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="dbService">The database service.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public AuthService(IDatabaseService dbService, IConfiguration configuration, IMapper mapper)
    {
        _dbService = dbService;
        _configuration = configuration;
        _mapper = mapper;
    }
    
    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterModel registration)
    {
        // Check if user already exists
        var existingUser = await _dbService.GetUserByEmailAsync(registration.Email);
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
        var createdUser = await _dbService.CreateUserAsync(user);
        
        // Generate JWT token
        var (token, expiresAt) = GenerateJwtToken(createdUser);
        
        // Map user to DTO
        var userDto = MapUserToDto(createdUser);
        
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
        var user = await _dbService.GetUserByEmailAsync(login.Email);
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
        user.LastLoginAt = DateTime.UtcNow;
        await _dbService.UpdateUserAsync(user);
        
        // Generate JWT token
        var (token, expiresAt) = GenerateJwtToken(user);
        
        // Map user to DTO
        var userDto = MapUserToDto(user);
        
        // Return authentication response
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = userDto
        };
    }
    
    /// <inheritdoc />
    public (string token, DateTime expiresAt) GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key not configured");
        var jwtIssuer = _configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer not configured");
        var jwtAudience = _configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience not configured");
        var jwtExpiryHours = int.Parse(_configuration["JWT:ExpiryInHours"] ?? "24");
        
        var key = Encoding.ASCII.GetBytes(jwtKey);
        var tokenHandler = new JwtSecurityTokenHandler();
        var expiresAt = DateTime.UtcNow.AddHours(jwtExpiryHours);
        
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
    public UserDto MapUserToDto(User user)
    {
        if (_mapper != null)
        {
            return _mapper.Map<UserDto>(user);
        }
        
        var dto = new UserDto
        {
            Id = user.Id,
            Name = user.DisplayName,
            Email = user.Email
        };
        
        if (user.PersonalInfo != null)
        {
            dto.PersonalInfo = new UserPersonalInfoDto
            {
                Address = user.PersonalInfo.Address,
                PhoneNumber = user.PersonalInfo.PhoneNumber,
                DateOfBirth = user.PersonalInfo.DateOfBirth
            };
        }
        
        if (user.Preferences != null)
        {
            dto.Preferences = new UserPreferencesDto
            {
                RiskTolerance = user.Preferences.RiskTolerance,
                InvestmentHorizon = user.Preferences.InvestmentHorizon
            };
        }
        
        return dto;
    }
} 