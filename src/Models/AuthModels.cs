namespace Invaise.BusinessDomain.API.Models;

/// <summary>
/// Model for user login requests containing email and password credentials
/// </summary>
public class LoginModel
{
    /// <summary>
    /// The user's email address used for authentication
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Model for refresh token requests to extend user session
/// </summary>
public class RefreshModel
{
    /// <summary>
    /// The refresh token used to generate a new access token
    /// </summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Model for user registration requests containing required user information
/// </summary>
public class RegisterModel
{
    /// <summary>
    /// The user's full name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's email address (must be unique)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's password (should meet security requirements)
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Model for service account authentication using ID and API key
/// </summary>
public class ServiceLoginModel
{
    /// <summary>
    /// The service account identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The service account API key for authentication
    /// </summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Model for updating user personal information
/// </summary>
public class UserPersonalInfoModel
{
    /// <summary>
    /// The user's physical address (optional)
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// The user's phone number (optional)
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// The user's date of birth (optional)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
}

/// <summary>
/// Response model for successful authentication containing token and user data
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// The JWT access token for authenticated requests
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// The expiration timestamp of the access token
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// The authenticated user's information
    /// </summary>
    public UserDto User { get; set; } = new UserDto();
}

/// <summary>
/// Data transfer object containing user information for API responses
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's full name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's role in the system (e.g., "User", "Admin")
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the user account is active and can authenticate
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// The timestamp of the user's last successful login
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    
    /// <summary>
    /// The user's personal information (optional)
    /// </summary>
    public UserPersonalInfoDto? PersonalInfo { get; set; }
    
    /// <summary>
    /// The user's investment preferences (optional)
    /// </summary>
    public UserPreferencesDto? Preferences { get; set; }
}

/// <summary>
/// Model for creating a new service account with specified permissions
/// </summary>
public class CreateServiceAccountDto
{
    /// <summary>
    /// The name of the service account
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The list of permissions granted to the service account
    /// </summary>
    public string[] Permissions { get; set; } = [];
}

/// <summary>
/// Data transfer object containing service account information including the unhashed API key
/// </summary>
public class ServiceAccountDto
{
    /// <summary>
    /// The unique identifier of the service account
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the service account
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The unhashed API key (only returned upon creation)
    /// </summary>
    public string KeyUnhashed { get; set; } = string.Empty;
    
    /// <summary>
    /// The role of the service account (typically "Service")
    /// </summary>
    public string Role { get; set; } = "Service";
    
    /// <summary>
    /// The list of permissions granted to the service account
    /// </summary>
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Data transfer object containing user personal information
/// </summary>
public class UserPersonalInfoDto
{
    /// <summary>
    /// The user's physical address
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// The user's phone number
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// The user's date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
}

/// <summary>
/// Data transfer object containing user investment preferences
/// </summary>
public class UserPreferencesDto
{
    /// <summary>
    /// The user's risk tolerance level (scale varies by implementation)
    /// </summary>
    public int RiskTolerance { get; set; }
    
    /// <summary>
    /// The user's investment time horizon (e.g., "Short", "Medium", "Long")
    /// </summary>
    public string InvestmentHorizon { get; set; } = string.Empty;
} 