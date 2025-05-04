using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Models;

/// <summary>
/// Model containing data for generating a portfolio performance PDF report
/// </summary>
public class PortfolioPerformanceReportModel
{
    /// <summary>
    /// Gets or sets user information for the report
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the portfolio
    /// </summary>
    public Portfolio Portfolio { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the current portfolio stocks
    /// </summary>
    public IEnumerable<PortfolioStock> PortfolioStocks { get; set; } = Array.Empty<PortfolioStock>();
    
    /// <summary>
    /// Gets or sets the portfolio performance data for the requested date range
    /// </summary>
    public IEnumerable<PortfolioPerformance> PerformanceData { get; set; } = Array.Empty<PortfolioPerformance>();
    
    /// <summary>
    /// Gets or sets the start date of the report period
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Gets or sets the end date of the report period
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Gets or sets the report generation date
    /// </summary>
    public DateTime GenerationDate { get; set; } = DateTime.UtcNow.ToLocalTime();
} 