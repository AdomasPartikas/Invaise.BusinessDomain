namespace Invaise.BusinessDomain.API.Models;

public class LoginModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ServiceLoginModel
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

public class UserPersonalInfoModel
{
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new UserDto();
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserPersonalInfoDto? PersonalInfo { get; set; }
    public UserPreferencesDto? Preferences { get; set; }
}

public class CreateServiceAccountDto
{
    public string Name { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = [];
}

public class ServiceAccountDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string KeyUnhashed { get; set; } = string.Empty;
    public string Role { get; set; } = "Service";
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

public class UserPersonalInfoDto
{
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class UserPreferencesDto
{
    public int RiskTolerance { get; set; }
    public string InvestmentHorizon { get; set; } = string.Empty;
} 