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
    /// <param name="userId">Optional user ID for personalized predictions</param>
    /// <returns>A Heat prediction</returns>
    Task<Heat?> GetHeatPredictionAsync(string symbol, string? userId = null);
    
    /// <summary>
    /// Gets combined heat predictions from Gaia for multiple symbols
    /// </summary>
    /// <param name="symbols">List of stock symbols</param>
    /// <param name="userId">Optional user ID for personalized predictions</param>
    /// <returns>Dictionary of symbol to heat prediction</returns>
    Task<Dictionary<string, Heat>> GetHeatPredictionsAsync(IEnumerable<string> symbols, string? userId = null);
    
    /// <summary>
    /// Optimizes a portfolio based on Gaia's predictions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="symbols">The symbols in the portfolio</param>
    /// <param name="riskTolerance">Optional risk tolerance factor</param>
    /// <returns>Portfolio optimization recommendations</returns>
    Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string userId, IEnumerable<string> symbols);
    
    /// <summary>
    /// Adjusts the weights of Apollo and Ignis models in the Gaia ensemble
    /// </summary>
    /// <param name="apolloWeight">Weight for Apollo model</param>
    /// <param name="ignisWeight">Weight for Ignis model</param>
    /// <returns>True if successful</returns>
    Task<bool> AdjustModelWeightsAsync(double apolloWeight, double ignisWeight);
} 