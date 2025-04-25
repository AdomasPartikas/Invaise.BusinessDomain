using System.Collections.Generic;

namespace Invaise.BusinessDomain.API.Models;

/// <summary>
/// Represents the results of a portfolio optimization from Gaia
/// </summary>
public class PortfolioOptimizationResult
{
    /// <summary>
    /// The user ID associated with this optimization
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// List of recommendations for the portfolio
    /// </summary>
    public List<PortfolioRecommendation> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Explanation of the optimization strategy
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence level in the optimization (0-1)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Timestamp when the optimization was performed
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Successful { get; set; } = true;
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a specific recommendation for a stock in the portfolio
/// </summary>
public class PortfolioRecommendation
{
    /// <summary>
    /// The stock symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Recommended action (buy, sell, hold)
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Current quantity in the portfolio
    /// </summary>
    public decimal CurrentQuantity { get; set; }
    
    /// <summary>
    /// Recommended quantity to target
    /// </summary>
    public decimal TargetQuantity { get; set; }
    
    /// <summary>
    /// Current weight in the portfolio (percentage)
    /// </summary>
    public double CurrentWeight { get; set; }
    
    /// <summary>
    /// Recommended weight in the portfolio (percentage)
    /// </summary>
    public double TargetWeight { get; set; }
    
    /// <summary>
    /// Explanation for this specific recommendation
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
} 