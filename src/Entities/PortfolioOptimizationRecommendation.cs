using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents an individual recommendation within a portfolio optimization
/// </summary>
public class PortfolioOptimizationRecommendation
{
    /// <summary>
    /// Gets or sets the unique identifier for the recommendation
    /// </summary>
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Gets or sets the optimization ID this recommendation belongs to
    /// </summary>
    [Required]
    public string OptimizationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the stock symbol
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the recommended action (buy, sell, hold)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the current quantity in the portfolio
    /// </summary>
    [Required]
    public decimal CurrentQuantity { get; set; }
    
    /// <summary>
    /// Gets or sets the recommended quantity to target
    /// </summary>
    [Required]
    public decimal TargetQuantity { get; set; }
    
    /// <summary>
    /// Gets or sets the current weight in the portfolio (percentage)
    /// </summary>
    public double CurrentWeight { get; set; }
    
    /// <summary>
    /// Gets or sets the recommended weight in the portfolio (percentage)
    /// </summary>
    public double TargetWeight { get; set; }
    
    /// <summary>
    /// Gets or sets the explanation for this specific recommendation
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Navigation property to the related optimization
    /// </summary>
    [ForeignKey("OptimizationId")]
    public virtual PortfolioOptimization Optimization { get; set; } = null!;
} 