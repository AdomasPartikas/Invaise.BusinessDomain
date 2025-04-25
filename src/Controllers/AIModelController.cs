using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for managing AI models
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AIModelController(IAIModelService aiModelService) : ControllerBase
{
    /// <summary>
    /// Creates a new AI model
    /// </summary>
    /// <param name="model">The model to create</param>
    /// <returns>The created model</returns>
    [HttpPost]
    public async Task<ActionResult<AIModel>> CreateModel([FromBody] AIModel model)
    {
        try
        {
            var createdModel = await aiModelService.CreateModelAsync(model);
            return CreatedAtAction(nameof(GetModelById), new { id = createdModel.Id }, createdModel);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating AI model: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all AI models
    /// </summary>
    /// <returns>Collection of AI models</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AIModel>>> GetAllModels()
    {
        try
        {
            var models = await aiModelService.GetAllModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving AI models: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets an AI model by ID
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <returns>The AI model if found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<AIModel>> GetModelById(long id)
    {
        try
        {
            var model = await aiModelService.GetModelByIdAsync(id);
            if (model == null)
            {
                return NotFound($"AI model with ID {id} not found");
            }
            return Ok(model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving AI model: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets AI models by status
    /// </summary>
    /// <param name="status">The status to filter by</param>
    /// <returns>Collection of AI models with the specified status</returns>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<AIModel>>> GetModelsByStatus(AIModelStatus status)
    {
        try
        {
            var models = await aiModelService.GetModelsByStatusAsync(status);
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving AI models by status: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing AI model
    /// </summary>
    /// <param name="model">The model with updated properties</param>
    /// <returns>True if successful</returns>
    [HttpPut]
    public async Task<ActionResult<bool>> UpdateModel([FromBody] AIModel model)
    {
        try
        {
            var success = await aiModelService.UpdateModelAsync(model);
            if (!success)
            {
                return NotFound($"AI model with ID {model.Id} not found");
            }
            return Ok(success);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating AI model: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a model's status
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <param name="status">The new status</param>
    /// <returns>True if successful</returns>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<bool>> UpdateModelStatus(long id, [FromBody] AIModelStatus status)
    {
        try
        {
            var success = await aiModelService.UpdateModelStatusAsync(id, status);
            if (!success)
            {
                return NotFound($"AI model with ID {id} not found");
            }
            return Ok(success);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating AI model status: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a model's training date
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <param name="trainedAt">The training timestamp</param>
    /// <returns>True if successful</returns>
    [HttpPut("{id}/training-date")]
    public async Task<ActionResult<bool>> UpdateModelTrainingDate(long id, [FromBody] DateTime trainedAt)
    {
        try
        {
            var success = await aiModelService.UpdateModelTrainingDateAsync(id, trainedAt);
            if (!success)
            {
                return NotFound($"AI model with ID {id} not found");
            }
            return Ok(success);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating AI model training date: {ex.Message}");
        }
    }
}
