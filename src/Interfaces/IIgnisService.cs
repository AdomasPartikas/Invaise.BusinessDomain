using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for interacting with the Ignis AI model
/// </summary>
public interface IIgnisService
{
    /// <summary>
    /// Gets the health status of the Ignis model
    /// </summary>
    /// <returns>True if the model is healthy, false otherwise</returns>
    Task<bool> CheckHealthAsync();
    
    /// <summary>
    /// Gets heat prediction for a specific stock
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Heat prediction details</returns>
    Task<(Heat, double)?> GetHeatPredictionAsync(string symbol);
    
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