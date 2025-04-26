using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for interacting with the Gaia AI model
/// </summary>
public interface IGaiaService
{
    /// <summary>
    /// Gets the health status of the Gaia model
    /// </summary>
    /// <returns>True if the model is healthy, false otherwise</returns>
    Task<bool> CheckHealthAsync();
    
    /// <summary>
    /// Gets the current version of the model
    /// </summary>
    /// <returns>The model version</returns>
    Task<string> GetModelVersionAsync();
    
    /// <summary>
    /// Gets a combined heat prediction from Gaia for a given symbol
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="portfolioId">PortfolioId for extracting users portfolio settings</param>
    /// <returns>A Heat prediction</returns>
    Task<(Heat, DateTime, double)?> GetHeatPredictionAsync(string symbol, string portfolioId);
    
    /// <summary>
    /// Optimizes a portfolio based on Gaia's predictions
    /// </summary>
    /// <param name="portfolioId">The portfolio that's being optimized</param>
    /// <returns>Portfolio optimization recommendations</returns>
    Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string portfolioId);
    
    /// <summary>
    /// Adjusts the weights of Apollo and Ignis models in the Gaia ensemble
    /// </summary>
    /// <param name="apolloWeight">Weight for Apollo model</param>
    /// <param name="ignisWeight">Weight for Ignis model</param>
    /// <returns>True if successful</returns>
    Task<bool> AdjustModelWeightsAsync(double apolloWeight, double ignisWeight);
} 