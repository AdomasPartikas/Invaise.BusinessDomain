namespace Invaise.BusinessDomain.API.Enums;

/// <summary>
/// Represents the status of a portfolio optimization
/// </summary>
public enum PortfolioOptimizationStatus
{
    /// <summary>
    /// Optimization has been completed successfully
    /// </summary>
    Created,

    /// <summary>
    /// Optimization is in progress
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Optimization has been canceled
    /// </summary>
    Canceled,
    
    /// <summary>
    /// Optimization has failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Optimization has been applied to the portfolio
    /// </summary>
    Applied
} 