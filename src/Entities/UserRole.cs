using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents the association between a user and a role within the system.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Gets or sets the unique identifier for this UserRole.
    /// </summary>
    [Key] // Marks this property as the primary key
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user associated with this role.
    /// </summary>
    [Required]
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the role assigned to the user.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;
}
