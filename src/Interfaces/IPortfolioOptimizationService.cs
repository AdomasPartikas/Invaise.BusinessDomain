using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for portfolio optimization service
/// </summary>
public interface IPortfolioOptimizationService
{
    /// <summary>
    /// Optimizes a user's portfolio based on predictions from Gaia
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">Optional portfolio ID (uses default portfolio if not specified)</param>
    /// <param name="riskTolerance">Optional risk tolerance factor</param>
    /// <returns>Portfolio optimization results</returns>
    Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string userId, string? portfolioId = null);
    
    /// <summary>
    /// Gets the optimization history for a user's portfolio
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">Optional portfolio ID (uses default portfolio if not specified)</param>
    /// <param name="startDate">Start date for history</param>
    /// <param name="endDate">End date for history</param>
    /// <returns>List of historical optimization results</returns>
    Task<IEnumerable<PortfolioOptimizationResult>> GetOptimizationHistoryAsync(
        string userId, string? portfolioId = null, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Applies optimization recommendations to a portfolio
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="optimizationId">The optimization result ID to apply</param>
    /// <returns>Result of the operation</returns>
    Task<PortfolioOptimizationResult> ApplyOptimizationRecommendationAsync(string userId, string optimizationId);
} 