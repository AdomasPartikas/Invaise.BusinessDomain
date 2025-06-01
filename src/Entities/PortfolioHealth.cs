using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents the health metrics of a portfolio, including financial and risk-adjusted metrics.
/// </summary>
public class PortfolioHealth
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio health record.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the portfolio.
    /// </summary>
    [Required]
    public required string PortfolioId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp indicating when the portfolio health metrics were recorded.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the total value of the portfolio.
    /// </summary>
    [Required]
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Gets or sets the total profit or loss of the portfolio.
    /// </summary>
    [Required]
    public decimal TotalProfitLoss { get; set; }

    /// <summary>
    /// Gets or sets the volatility of the portfolio, representing the degree of variation in its value over time.
    /// </summary>
    public decimal? Volatility { get; set; }

    /// <summary>
    /// Gets or sets the Sharpe Ratio of the portfolio, representing the risk-adjusted return.
    /// </summary>
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the diversification score of the portfolio, representing the degree of asset diversification.
    /// </summary>
    public decimal? DiversificationScore { get; set; }

    /// <summary>
    /// Gets or sets the maximum drawdown of the portfolio, representing the largest peak-to-trough decline in value.
    /// </summary>
    public decimal? MaxDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the risk-adjusted return of the portfolio, representing the return adjusted for risk exposure.
    /// </summary>
    public decimal RiskAdjustedReturn { get; set; }

    /// <summary>
    /// Gets or sets the portfolio associated with the portfolio health metrics.
    /// </summary>
    public virtual Portfolio Portfolio { get; set; } = null!;
}