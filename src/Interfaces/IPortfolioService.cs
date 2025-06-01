using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for managing portfolios and their stocks
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Refreshes the calculated values and metrics for all portfolios in the system.
    /// Updates portfolio performance, valuations, and other derived metrics.
    /// </summary>
    /// <returns>A task that represents the asynchronous refresh operation</returns>
    Task RefreshAllPortfoliosAsync();
    
    /// <summary>
    /// Saves end-of-day performance data for all portfolios
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveEodPortfolioPerformanceAsync();
    
    /// <summary>
    /// Generates a PDF report of portfolio performance for a specific date range
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="startDate">The start date for performance data</param>
    /// <param name="endDate">The end date for performance data</param>
    /// <returns>The generated PDF file as a byte array</returns>
    Task<byte[]> GeneratePortfolioPerformancePdfAsync(string userId, string portfolioId, DateTime startDate, DateTime endDate);
} 