using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for managing portfolios and their stocks
/// </summary>
public interface IPortfolioService
{
    Task RefreshAllPortfoliosAsync();
    
    /// <summary>
    /// Saves end-of-day performance data for all portfolios
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveEodPortfolioPerformanceAsync();
} 