using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;


/// <summary>
/// Represents an AI model with various properties such as name, status, and timestamps.
/// </summary>
public class AIModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the AI model.
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the AI model.
    /// This field is required and has a maximum length of 255 characters.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access URL for the AI model.
    /// This field is optional and has a maximum length of 2048 characters.
    /// </summary>
    [MaxLength(2048)]
    public string? AccessUrl { get; set; }

    /// <summary>
    /// Gets or sets the description of the AI model.
    /// This field is optional and has a maximum length of 1000 characters.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the status of the AI model.
    /// This field is required and defaults to "ACTIVE".
    /// </summary>
    [Required]
    public AIModelStatus ModelStatus { get; set; } = AIModelStatus.Active;

    /// <summary>
    /// Gets or sets the date and time when the AI model was created.
    /// This field defaults to the current UTC date and time.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the date and time when the AI model was last updated.
    /// This field is optional.
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the AI model was last trained.
    /// This field is optional.
    /// </summary>
    public DateTime? LastTrainedAt { get; set; }

    /// <summary>
    /// Gets or sets the current version of the AI model.
    /// This field is optional.
    /// </summary>
    [MaxLength(100)]
    public string? Version { get; set; }
}