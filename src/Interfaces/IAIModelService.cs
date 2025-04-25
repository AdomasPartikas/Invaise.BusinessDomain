using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for AI model management and operations
/// </summary>
public interface IAIModelService
{
    /// <summary>
    /// Gets all AI models
    /// </summary>
    /// <returns>Collection of AI models</returns>
    Task<IEnumerable<AIModel>> GetAllModelsAsync();
    
    /// <summary>
    /// Gets an AI model by its ID
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <returns>The AI model if found, null otherwise</returns>
    Task<AIModel?> GetModelByIdAsync(long id);
    
    /// <summary>
    /// Gets AI models by status
    /// </summary>
    /// <param name="status">The status to filter by</param>
    /// <returns>Collection of AI models with the specified status</returns>
    Task<IEnumerable<AIModel>> GetModelsByStatusAsync(AIModelStatus status);
    
    /// <summary>
    /// Updates a model's status
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <param name="status">The new status</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateModelStatusAsync(long id, AIModelStatus status);
    
    /// <summary>
    /// Updates a model's last trained timestamp
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <param name="trainedAt">The training timestamp</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateModelTrainingDateAsync(long id, DateTime trainedAt);
    
    /// <summary>
    /// Creates a new AI model
    /// </summary>
    /// <param name="model">The model to create</param>
    /// <returns>The created model with ID</returns>
    Task<AIModel> CreateModelAsync(AIModel model);
    
    /// <summary>
    /// Updates an existing AI model
    /// </summary>
    /// <param name="model">The model with updated properties</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateModelAsync(AIModel model);
} 