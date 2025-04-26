using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents the personal information of a user.
/// </summary>
public class UserPersonalInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the foreign key to the user.
    /// </summary>
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal first name of the user.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LegalFirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal last name of the user.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LegalLastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of birth of the user.
    /// </summary>
    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the user.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the government-issued ID of the user.
    /// </summary>
    [MaxLength(20)]
    public string? GovernmentId { get; set; }

    /// <summary>
    /// Gets or sets the primary address line of the user.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city of the user's address.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code of the user's address.
    /// </summary>
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the country of the user's address.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the date and time when the record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}