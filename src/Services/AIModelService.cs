using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for managing AI models
/// </summary>
public class AIModelService(InvaiseDbContext dbContext, Serilog.ILogger logger) : IAIModelService
{
    /// <inheritdoc/>
    public async Task<AIModel> CreateModelAsync(AIModel model)
    {
        try
        {
            dbContext.AIModels.Add(model);
            await dbContext.SaveChangesAsync();
            return model;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error creating AI model: {ModelName}", model.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AIModel>> GetAllModelsAsync()
    {
        return await dbContext.AIModels.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<AIModel?> GetModelByIdAsync(long id)
    {
        return await dbContext.AIModels.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AIModel>> GetModelsByStatusAsync(AIModelStatus status)
    {
        return await dbContext.AIModels
            .Where(m => m.ModelStatus == status)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelAsync(AIModel model)
    {
        try
        {
            model.LastUpdated = DateTime.UtcNow.ToLocalTime();
            dbContext.AIModels.Update(model);
            var affected = await dbContext.SaveChangesAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error updating AI model: {ModelId}", model.Id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelStatusAsync(long id, AIModelStatus status)
    {
        try
        {
            var model = await dbContext.AIModels.FindAsync(id);
            if (model == null)
            {
                return false;
            }

            model.ModelStatus = status;
            model.LastUpdated = DateTime.UtcNow.ToLocalTime();
            
            var affected = await dbContext.SaveChangesAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error updating AI model status: {ModelId}", id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelTrainingDateAsync(long id, DateTime trainedAt)
    {
        try
        {
            var model = await dbContext.AIModels.FindAsync(id);
            if (model == null)
            {
                return false;
            }

            model.LastTrainedAt = trainedAt;
            model.LastUpdated = DateTime.UtcNow.ToLocalTime();
            
            var affected = await dbContext.SaveChangesAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error updating AI model training date: {ModelId}", id);
            return false;
        }
    }
} 