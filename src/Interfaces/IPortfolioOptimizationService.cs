using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for portfolio optimization service
/// </summary>
public interface IPortfolioOptimizationService
{
    /// <summary>
    /// Checks if there's an ongoing optimization for the specified portfolio
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">Portfolio ID to check</param>
    /// <returns>True if there's an ongoing optimization, false otherwise</returns>
    Task<bool> HasOngoingOptimizationAsync(string userId, string portfolioId);
    
    /// <summary>
    /// Gets the status of an optimization
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="optimizationId">The optimization ID</param>
    /// <returns>The current status of the optimization</returns>
    Task<PortfolioOptimizationStatus> GetOptimizationStatusAsync(string userId, string optimizationId);
    
    /// <summary>
    /// Gets all optimizations for a specific portfolio
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">Portfolio ID</param>
    /// <returns>List of optimization results for this portfolio</returns>
    Task<IEnumerable<PortfolioOptimizationResult>> GetOptimizationsByPortfolioAsync(string userId, string portfolioId);
    
    /// <summary>
    /// Optimizes a user's portfolio based on predictions from Gaia
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">Portfolio ID to be optimized</param>
    /// <returns>Portfolio optimization results</returns>
    Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string userId, string portfolioId);
    
    /// <summary>
    /// Cancels existing optimizations for portfolios containing the specified symbols
    /// when new heat predictions are generated
    /// </summary>
    /// <param name="symbols">The stock symbols with new heat predictions</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> CancelExistingOptimizationsForSymbolsAsync(IEnumerable<string> symbols);
    
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
    
    /// <summary>
    /// Cancels an in-progress optimization
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="optimizationId">The optimization ID to cancel</param>
    /// <returns>Result of the operation</returns>
    Task<PortfolioOptimizationResult> CancelOptimizationAsync(string userId, string optimizationId);

    /// <summary>
    /// Ensures that all in-progress optimizations are completed
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task EnsureCompletionOfAllInProgressOptimizationsAsync();

    /// <summary>
    /// Gets the remaining cooldown time before a new optimization can be started for the specified portfolio
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>The remaining cooldown time as a TimeSpan</returns>
    Task<TimeSpan> GetRemainingCoolOffTime(string userId, string portfolioId);
} 