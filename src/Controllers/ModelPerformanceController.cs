using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for managing AI model performance and training
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelPerformanceController : ControllerBase
{
    private readonly IModelPerformanceService _modelPerformanceService;
    private readonly IAIModelService _aiModelService;
    
    /// <summary>
    /// Initializes a new instance of the ModelPerformanceController class
    /// </summary>
    public ModelPerformanceController(IModelPerformanceService modelPerformanceService, IAIModelService aiModelService)
    {
        _modelPerformanceService = modelPerformanceService;
        _aiModelService = aiModelService;
    }
    
    /// <summary>
    /// Gets training status of all models currently in training
    /// </summary>
    /// <returns>Dictionary mapping model IDs to their training status</returns>
    [HttpGet("training-status")]
    public async Task<ActionResult<Dictionary<long, bool>>> GetTrainingStatus()
    {
        try
        {
            var results = await _modelPerformanceService.CheckTrainingModelsStatusAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error checking training status: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets whether a specific model needs retraining
    /// </summary>
    /// <param name="modelId">The AI model ID</param>
    /// <returns>True if model needs retraining, false otherwise</returns>
    [HttpGet("{modelId}/needs-retraining")]
    public async Task<ActionResult<bool>> CheckIfNeedsRetraining(long modelId)
    {
        try
        {
            var result = await _modelPerformanceService.CheckIfModelNeedsRetrainingAsync(modelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error checking if model needs retraining: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Initiates retraining for a specific model
    /// </summary>
    /// <param name="modelId">The AI model ID</param>
    /// <returns>True if retraining initiated successfully, false otherwise</returns>
    [HttpPost("{modelId}/retrain")]
    public async Task<ActionResult<bool>> InitiateRetraining(long modelId)
    {
        try
        {
            // First check if model exists
            var model = await _aiModelService.GetModelByIdAsync(modelId);
            if (model == null)
            {
                return NotFound($"Model with ID {modelId} not found");
            }
            
            // Check if model can be retrained (Apollo or Ignis only)
            if (model.Name.ToLower() != "apollo" && model.Name.ToLower() != "ignis")
            {
                return BadRequest($"Model {model.Name} does not support retraining");
            }
            
            // Check if model is already training
            if (model.ModelStatus == AIModelStatus.Training)
            {
                return BadRequest($"Model {model.Name} is already training");
            }
            
            var result = await _modelPerformanceService.InitiateModelRetrainingAsync(modelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error initiating model retraining: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks all models to determine if any need retraining, and initiates retraining if needed
    /// </summary>
    /// <returns>Dictionary mapping model IDs to whether retraining was initiated</returns>
    [HttpPost("check-and-retrain-all")]
    public async Task<ActionResult<Dictionary<long, bool>>> CheckAndRetrainAllModels()
    {
        try
        {
            var results = await _modelPerformanceService.CheckAndInitiateRetrainingForAllModelsAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error checking and retraining models: {ex.Message}");
        }
    }
} 