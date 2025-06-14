using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a user entity in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the role of the user (e.g., Admin, User).
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user (hashed).
    /// </summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the date and time when the user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user's email is verified.
    /// </summary>
    [Required]
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the user preferences.
    /// </summary>
    public UserPreferences? Preferences { get; set; }

    /// <summary>
    /// Gets or sets the personal information of the user.
    /// </summary>
    public UserPersonalInfo? PersonalInfo { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of portfolios associated with the user.
    /// </summary>
    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();

    /// <summary>
    /// Gets or sets the collection of transactions associated with the user.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new List<Transaction>();
}