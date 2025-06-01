using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents user preferences, including risk tolerance, investment horizon, and timestamps.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the unique identifier for the user preferences.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the unique identifier for the associated user.
    /// </summary>
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the risk tolerance level of the user. Default is medium risk (scale 1-10).
    /// </summary>
    public int RiskTolerance { get; set; } = 5; // Default to medium risk (scale 1-10)

    /// <summary>
    /// Gets or sets the investment horizon of the user. Default is "Medium" (Short, Medium, Long).
    /// </summary>
    public string InvestmentHorizon { get; set; } = "Medium"; // Short, Medium, Long

    /// <summary>
    /// Gets or sets the timestamp when the user preferences were created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the timestamp when the user preferences were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    
    // Navigation property
    /// <summary>
    /// Gets or sets the associated user entity.
    /// </summary>
    public virtual User User { get; set; } = null!;
} 