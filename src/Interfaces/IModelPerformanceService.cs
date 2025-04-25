using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for monitoring and managing AI model performance and training
/// </summary>
public interface IModelPerformanceService
{
    /// <summary>
    /// Checks if a model needs to be retrained based on its last training date and performance metrics
    /// </summary>
    /// <param name="modelId">The AI model ID</param>
    /// <returns>True if the model needs retraining, false otherwise</returns>
    Task<bool> CheckIfModelNeedsRetrainingAsync(long modelId);
    
    /// <summary>
    /// Initiates retraining for a model
    /// </summary>
    /// <param name="modelId">The AI model ID</param>
    /// <returns>True if retraining was successfully initiated, false otherwise</returns>
    Task<bool> InitiateModelRetrainingAsync(long modelId);
    
    /// <summary>
    /// Checks training status of models currently in Training state and updates status when complete
    /// </summary>
    /// <returns>Dictionary mapping model IDs to their current training status</returns>
    Task<Dictionary<long, bool>> CheckTrainingModelsStatusAsync();
    
    /// <summary>
    /// Checks all trainable models to determine if any need retraining
    /// </summary>
    /// <returns>Dictionary mapping model IDs to whether retraining was initiated</returns>
    Task<Dictionary<long, bool>> CheckAndInitiateRetrainingForAllModelsAsync();
} 