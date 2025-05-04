using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Models;

/// <summary>
/// Model for forgot password requests.
/// </summary>
public class ForgotPasswordModel
{
    /// <summary>
    /// The email of the user requesting a password reset.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
} 