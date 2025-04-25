using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.Extensions.Logging;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for monitoring and managing AI model performance and training
/// </summary>
public class ModelPerformanceService(
        IAIModelService aiModelService, 
        IApolloService apolloService, 
        IIgnisService ignisService, 
        Serilog.ILogger logger) : IModelPerformanceService
{
    
    // Default training frequency in days
    private const int DEFAULT_RETRAINING_INTERVAL_DAYS = 1;
    
    /// <inheritdoc/>
    public async Task<bool> CheckIfModelNeedsRetrainingAsync(long modelId)
    {
        var model = await aiModelService.GetModelByIdAsync(modelId);
        if (model == null)
        {
            logger.Warning("Model not found: {ModelId}", modelId);
            return false;
        }
        
        // Check if model is currently training
        if (model.ModelStatus == AIModelStatus.Training)
        {
            logger.Information("Model {ModelId} is already training", modelId);
            return false;
        }
        
        // If model hasn't been trained before, it needs training
        if (!model.LastTrainedAt.HasValue)
        {
            logger.Information("Model {ModelId} has never been trained", modelId);
            return true;
        }
        
        // Check if enough time has passed since the last training
        var daysSinceLastTraining = (DateTime.UtcNow - model.LastTrainedAt.Value).TotalDays;
        if (daysSinceLastTraining >= DEFAULT_RETRAINING_INTERVAL_DAYS)
        {
            logger.Information("Model {ModelId} was last trained {Days} days ago, exceeding the threshold of {Threshold} days", 
                modelId, Math.Round(daysSinceLastTraining, 1), DEFAULT_RETRAINING_INTERVAL_DAYS);
            return true;
        }
        
        return false;
    }
    
    /// <inheritdoc/>
    public async Task<bool> InitiateModelRetrainingAsync(long modelId)
    {
        var model = await aiModelService.GetModelByIdAsync(modelId);
        if (model == null)
        {
            logger.Warning("Model not found: {ModelId}", modelId);
            return false;
        }
        
        // Only Apollo and Ignis models can be retrained
        if (model.Name.ToLower() != "apollo" && model.Name.ToLower() != "ignis")
        {
            logger.Warning("Model {ModelId} ({ModelName}) is not supported for retraining", modelId, model.Name);
            return false;
        }
        
        // Check if model is already in training state
        if (model.ModelStatus == AIModelStatus.Training)
        {
            logger.Information("Model {ModelId} is already training", modelId);
            return false;
        }
        
        bool retrainingRequested = false;
        
        try
        {
            // Request retraining based on model type
            if (model.Name.ToLower() == "apollo")
            {
                retrainingRequested = await apolloService.RequestRetrainingAsync();
            }
            else if (model.Name.ToLower() == "ignis")
            {
                retrainingRequested = await ignisService.RequestRetrainingAsync();
            }
            
            // If retraining was successfully requested, update model status
            if (retrainingRequested)
            {
                await aiModelService.UpdateModelStatusAsync(modelId, AIModelStatus.Training);
                logger.Information("Model {ModelId} retraining initiated", modelId);
            }
            else
            {
                logger.Warning("Failed to initiate retraining for model {ModelId}", modelId);
            }
            
            return retrainingRequested;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error initiating retraining for model {ModelId}", modelId);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<long, bool>> CheckTrainingModelsStatusAsync()
    {
        var results = new Dictionary<long, bool>();
        
        // Get all models currently in Training state
        var trainingModels = await aiModelService.GetModelsByStatusAsync(AIModelStatus.Training);
        
        if (!trainingModels.Any())
        {
            logger.Debug("No models currently in training state");
            return results;
        }
        
        foreach (var model in trainingModels)
        {
            bool isStillTraining = false;
            
            try
            {
                // Check training status based on model type
                if (model.Name.ToLower() == "apollo")
                {
                    isStillTraining = await apolloService.IsTrainingAsync();
                }
                else if (model.Name.ToLower() == "ignis")
                {
                    isStillTraining = await ignisService.IsTrainingAsync();
                }
                else
                {
                    // Skip models that don't support training
                    continue;
                }
                
                results[model.Id] = isStillTraining;
                
                // If model is no longer training, update its status to Active
                if (!isStillTraining)
                {
                    await aiModelService.UpdateModelStatusAsync(model.Id, AIModelStatus.Active);
                    await aiModelService.UpdateModelTrainingDateAsync(model.Id, DateTime.UtcNow);
                    logger.Information("Model {ModelId} training completed, status updated to Active", model.Id);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error checking training status for model {ModelId}", model.Id);
                results[model.Id] = false;
            }
        }
        
        return results;
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<long, bool>> CheckAndInitiateRetrainingForAllModelsAsync()
    {
        var results = new Dictionary<long, bool>();
        
        // Get all active models
        var models = await aiModelService.GetModelsByStatusAsync(AIModelStatus.Active);
        
        // Filter to get only Apollo and Ignis models
        var eligibleModels = models.Where(m => 
            m.Name.ToLower() == "apollo" || 
            m.Name.ToLower() == "ignis").ToList();
            
        if (!eligibleModels.Any())
        {
            logger.Debug("No eligible models found for retraining check");
            return results;
        }
        
        foreach (var model in eligibleModels)
        {
            try
            {
                bool needsRetraining = await CheckIfModelNeedsRetrainingAsync(model.Id);
                
                if (needsRetraining)
                {
                    bool retrainingInitiated = await InitiateModelRetrainingAsync(model.Id);
                    results[model.Id] = retrainingInitiated;
                }
                else
                {
                    results[model.Id] = false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error checking/initiating retraining for model {ModelId}", model.Id);
                results[model.Id] = false;
            }
        }
        
        return results;
    }
} 