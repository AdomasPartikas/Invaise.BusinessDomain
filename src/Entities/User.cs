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
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the company associated with the user.
    /// </summary>
    [Required]
    public int UserRoleId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
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
    /// Gets or sets the status of the user. Default is "ACTIVE".
    /// </summary>
    [Required]
    public string Status { get; set; } = "ACTIVE";

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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


    public virtual UserRole UserRole { get; set; } = null!;
    public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}