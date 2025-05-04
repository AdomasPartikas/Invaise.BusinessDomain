using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Entity that stores end-of-day performance data for portfolios
/// </summary>
public class PortfolioPerformance
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ForeignKey("Portfolio")]
    public string PortfolioId { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public decimal TotalValue { get; set; }

    [Required]
    public decimal DailyChangePercent { get; set; }

    public decimal? WeeklyChangePercent { get; set; }
    
    public decimal? MonthlyChangePercent { get; set; }

    public decimal? YearlyChangePercent { get; set; }

    public int TotalStocks { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    // Navigation property
    public virtual Portfolio Portfolio { get; set; } = null!;
} 