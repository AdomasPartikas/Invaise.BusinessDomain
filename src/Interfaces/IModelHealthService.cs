using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for monitoring health of AI models
/// </summary>
public interface IModelHealthService
{
    /// <summary>
    /// Checks the health of all registered AI models
    /// </summary>
    /// <returns>Dictionary mapping model IDs to health status (true=healthy)</returns>
    Task<Dictionary<long, bool>> CheckAllModelsHealthAsync();
    
    /// <summary>
    /// Checks the health of a specific AI model
    /// </summary>
    /// <param name="modelId">The AI model ID</param>
    /// <returns>True if the model is healthy, false otherwise</returns>
    Task<bool> CheckModelHealthAsync(long modelId);
    
    /// <summary>
    /// Updates the health status of a model in the database
    /// </summary>
    /// <param name="modelId">The AI model ID</param>
    /// <param name="isHealthy">The health status</param>
    /// <returns>True if update successful, false otherwise</returns>
    Task<bool> UpdateModelHealthStatusAsync(long modelId, bool isHealthy);
    
    // /// <summary>
    // /// Gets models that have been unhealthy for a specified duration
    // /// </summary>
    // /// <param name="thresholdHours">Hours threshold</param>
    // /// <returns>List of unhealthy models</returns>
    // Task<IEnumerable<AIModel>> GetPersistentlyUnhealthyModelsAsync(int thresholdHours);
} 