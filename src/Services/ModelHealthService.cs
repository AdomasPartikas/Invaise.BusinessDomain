using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.ApolloAPIClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Invaise.BusinessDomain.API.IgnisAPIClient;
using Invaise.BusinessDomain.API.GaiaAPIClient;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for monitoring the health of AI models
/// </summary>
public class ModelHealthService(IAIModelService aiModelService, IApolloService healthApolloService, IIgnisService healthIgnisService, IGaiaService healthGaiaService, Serilog.ILogger logger) : IModelHealthService
{

    /// <inheritdoc/>
    public async Task<Dictionary<long, bool>> CheckAllModelsHealthAsync()
    {
        var results = new Dictionary<long, bool>();
        var activeModels = await aiModelService.GetAllModelsAsync();

        if (activeModels == null || !activeModels.Any())
        {
            logger.Warning("No active models found for health check.");
            return results;
        }
        
        results = await Task.WhenAll(activeModels.Select(async model =>
        {
            bool isHealthy = false;

            if (model.ModelStatus == AIModelStatus.Training)
            {
                return new { model.Id, isHealthy };
            }

            try
            {
                isHealthy = await CheckModelHealthAsync(model.Id);
                await UpdateModelHealthStatusAsync(model.Id, isHealthy);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error checking health for model: {ModelId}", model.Id);
                isHealthy = false;
            }

            return new { model.Id, isHealthy };
        })).ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.isHealthy));
        
        return results;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckModelHealthAsync(long modelId)
    {
        var model = await aiModelService.GetModelByIdAsync(modelId);

        if (model == null)
        {
            logger.Warning("Model not found: {ModelId}", modelId);
            return false;
        }
        
        try
        {
            bool isHealthy = model.Name.ToLower() switch
            {
                "gaia" => await healthGaiaService.CheckHealthAsync(),
                "apollo" => await healthApolloService.CheckHealthAsync(),
                "ignis" => await healthIgnisService.CheckHealthAsync(),
                _ => false
            };
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            if(model.ModelStatus == AIModelStatus.Inactive)
            {
                logger.Debug("Model {ModelName} is inactive, skipping health check.", model.Name);
            }
            else
            {
                logger.Error(ex, "Error checking health for model: {ModelName}", model.Name);
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelHealthStatusAsync(long modelId, bool isHealthy)
    {
        var model = await aiModelService.GetModelByIdAsync(modelId);
        if (model == null)
        {
            return false;
        }
        
        // Update model status based on health check
        var newStatus = isHealthy ? AIModelStatus.Active : AIModelStatus.Inactive;
        
        // Only update if status changed
        if (model.ModelStatus != newStatus)
        {
            await aiModelService.UpdateModelStatusAsync(modelId, newStatus);
            logger.Information("Updated model {ModelId} status to {Status}", modelId, newStatus);
        }
        
        return true;
    }
} 