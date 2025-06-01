using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Entity that stores end-of-day performance data for portfolios
/// </summary>
public class PortfolioPerformance
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio performance.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the unique identifier for the portfolio associated with this performance.
    /// </summary>
    [ForeignKey("Portfolio")]
    public string PortfolioId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date for the portfolio performance.
    /// </summary>
    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Gets or sets the total value of the portfolio.
    /// </summary>
    [Required]
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Gets or sets the daily percentage change in the portfolio's value.
    /// </summary>
    [Required]
    public decimal DailyChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the weekly percentage change in the portfolio's value.
    /// </summary>
    public decimal? WeeklyChangePercent { get; set; }
    
    /// <summary>
    /// Gets or sets the monthly percentage change in the portfolio's value.
    /// </summary>
    public decimal? MonthlyChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the yearly percentage change in the portfolio's value.
    /// </summary>
    public decimal? YearlyChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the total number of stocks in the portfolio.
    /// </summary>
    public int TotalStocks { get; set; }
    
    /// <summary>
    /// Gets or sets the creation timestamp for the portfolio performance.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    // Navigation property
    /// <summary>
    /// Gets or sets the portfolio associated with this performance.
    /// </summary>
    public virtual Portfolio Portfolio { get; set; } = null!;
} 