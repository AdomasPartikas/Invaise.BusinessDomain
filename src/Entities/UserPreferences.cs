using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

public class UserPreferences
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    
    public int RiskTolerance { get; set; } = 5; // Default to medium risk (scale 1-10)
    public string InvestmentHorizon { get; set; } = "Medium"; // Short, Medium, Long
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
} 