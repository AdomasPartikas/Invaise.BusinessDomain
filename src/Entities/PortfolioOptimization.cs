using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a portfolio optimization result from Gaia
/// </summary>
public class PortfolioOptimization
{
    /// <summary>
    /// Gets or sets the unique identifier for the optimization
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the user ID associated with this optimization
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the portfolio ID
    /// </summary>
    [Required]
    public string PortfolioId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the timestamp when the optimization was performed
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the explanation of the optimization strategy
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the confidence level in the optimization (0-1)
    /// </summary>
    [Range(0, 1)]
    public double Confidence { get; set; }
    
    /// <summary>
    /// Gets or sets the risk tolerance used for this optimization
    /// </summary>
    public double? RiskTolerance { get; set; }
    
    /// <summary>
    /// Gets or sets a flag indicating if this optimization has been applied
    /// </summary>
    public bool IsApplied { get; set; }
    
    /// <summary>
    /// Gets or sets the date when this optimization was applied
    /// </summary>
    public DateTime? AppliedDate { get; set; }
    
    /// <summary>
    /// Gets or sets the model version used for this optimization
    /// </summary>
    [MaxLength(20)]
    public string ModelVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optimization recommendations
    /// </summary>
    public virtual ICollection<PortfolioOptimizationRecommendation> Recommendations { get; set; } = new List<PortfolioOptimizationRecommendation>();
    
    /// <summary>
    /// Navigation property to the related portfolio
    /// </summary>
    [ForeignKey("PortfolioId")]
    public virtual Portfolio Portfolio { get; set; } = null!;
} 