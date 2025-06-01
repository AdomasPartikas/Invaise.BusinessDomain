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
public class AIModelService : IAIModelService
{
    private readonly InvaiseDbContext _dbContext;
    private readonly Serilog.ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the AIModelService class
    /// </summary>
    /// <param name="dbContext">The database context for data access</param>
    /// <param name="logger">The logger for recording operations and errors</param>
    public AIModelService(InvaiseDbContext dbContext, Serilog.ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AIModel> CreateModelAsync(AIModel model)
    {
        try
        {
            _dbContext.AIModels.Add(model);
            await _dbContext.SaveChangesAsync();
            return model;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating AI model: {ModelName}", model.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AIModel>> GetAllModelsAsync()
    {
        return await _dbContext.AIModels.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<AIModel?> GetModelByIdAsync(long id)
    {
        return await _dbContext.AIModels.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AIModel>> GetModelsByStatusAsync(AIModelStatus status)
    {
        return await _dbContext.AIModels
            .Where(m => m.ModelStatus == status)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelAsync(AIModel model)
    {
        try
        {
            model.LastUpdated = DateTime.UtcNow.ToLocalTime();
            _dbContext.AIModels.Update(model);
            var affected = await _dbContext.SaveChangesAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating AI model: {ModelId}", model.Id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelStatusAsync(long id, AIModelStatus status)
    {
        try
        {
            var model = await _dbContext.AIModels.FindAsync(id);
            if (model == null)
            {
                return false;
            }

            model.ModelStatus = status;
            model.LastUpdated = DateTime.UtcNow.ToLocalTime();
            
            var affected = await _dbContext.SaveChangesAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating AI model status: {ModelId}", id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateModelTrainingDateAsync(long id, DateTime trainedAt)
    {
        try
        {
            var model = await _dbContext.AIModels.FindAsync(id);
            if (model == null)
            {
                return false;
            }

            model.LastTrainedAt = trainedAt;
            model.LastUpdated = DateTime.UtcNow.ToLocalTime();
            
            var affected = await _dbContext.SaveChangesAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating AI model training date: {ModelId}", id);
            return false;
        }
    }
} 