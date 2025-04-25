using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for interacting with the Apollo AI model
/// </summary>
public interface IApolloService
{
    /// <summary>
    /// Gets the health status of the Apollo model
    /// </summary>
    /// <returns>True if the model is healthy, false otherwise</returns>
    Task<bool> CheckHealthAsync();
    
    /// <summary>
    /// Gets heat prediction for a specific stock
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Heat prediction details</returns>
    Task<Heat?> GetHeatPredictionAsync(string symbol);
    
    /// <summary>
    /// Gets heat predictions for multiple stocks
    /// </summary>
    /// <param name="symbols">List of stock symbols</param>
    /// <returns>Dictionary mapping symbols to heat predictions</returns>
    Task<Dictionary<string, Heat>> GetHeatPredictionsAsync(IEnumerable<string> symbols);
    
    /// <summary>
    /// Requests model retraining
    /// </summary>
    /// <returns>True if retraining initiated successfully, false otherwise</returns>
    Task<bool> RequestRetrainingAsync();

    /// <summary>
    /// Checks if the model is currently training
    /// </summary>
    /// <returns>True if the model is training, false otherwise</returns>
    Task<bool> IsTrainingAsync();
    
    /// <summary>
    /// Gets the current model version information
    /// </summary>
    /// <returns>Version string</returns>
    Task<string> GetModelVersionAsync();
} 