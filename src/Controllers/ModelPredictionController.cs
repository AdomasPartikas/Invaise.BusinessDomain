using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for managing AI model predictions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelPredictionController(IModelPredictionService modelPredictionService) : ControllerBase
{
    /// <summary>
    /// Gets the latest prediction for a symbol from a specific model source
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="modelSource">The model source (e.g., "Apollo", "Ignis")</param>
    /// <returns>The latest prediction if available</returns>
    [HttpGet("{symbol}/{modelSource}")]
    public async Task<ActionResult<Prediction>> GetLatestPrediction(string symbol, string modelSource)
    {
        try
        {
            var prediction = await modelPredictionService.GetLatestPredictionAsync(symbol, modelSource);
            if (prediction == null)
            {
                return NotFound($"No prediction found for {symbol} from {modelSource}");
            }
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving prediction: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets all available predictions for a symbol from all model sources
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Dictionary mapping model sources to predictions</returns>
    [HttpGet("{symbol}/all")]
    public async Task<ActionResult<Dictionary<string, Prediction>>> GetAllLatestPredictions(string symbol)
    {
        try
        {
            var predictions = await modelPredictionService.GetAllLatestPredictionsAsync(symbol);
            if (predictions.Count == 0)
            {
                return NotFound($"No predictions found for {symbol}");
            }
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving predictions: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets historical predictions for a symbol from a specific model
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="modelSource">The source model</param>
    /// <param name="startDate">The start date for historical data (format: yyyy-MM-dd)</param>
    /// <param name="endDate">The end date for historical data (format: yyyy-MM-dd)</param>
    /// <returns>List of historical predictions</returns>
    [HttpGet("{symbol}/{modelSource}/history")]
    public async Task<ActionResult<IEnumerable<Prediction>>> GetHistoricalPredictions(
        string symbol, 
        string modelSource, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var predictions = await modelPredictionService.GetHistoricalPredictionsAsync(symbol, modelSource, startDate, endDate);
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving historical predictions: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Refreshes predictions for a symbol from all model sources
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Dictionary mapping model sources to refreshed predictions</returns>
    [HttpPost("{symbol}/refresh")]
    public async Task<ActionResult<Dictionary<string, Prediction>>> RefreshPredictions(string symbol)
    {
        try
        {
            var predictions = await modelPredictionService.RefreshPredictionsAsync(symbol);
            if (predictions.Count == 0)
            {
                return NotFound($"Failed to get predictions for {symbol}");
            }
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error refreshing predictions: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes predictions for a portfolio from Gaia
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>Dictionary mapping model sources to refreshed predictions</returns>
    [HttpPost("portfolio/{portfolioId}/refresh")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, Prediction>))]
    public async Task<ActionResult<Dictionary<string, Prediction>>> RefreshPortfolioPredictions(string portfolioId)
    {
        try
        {
            var predictions = await modelPredictionService.RefreshPortfolioPredictionsAsync(portfolioId);
            if (predictions.Count == 0)
            {
                return NotFound($"Failed to get predictions for portfolio {portfolioId}");
            }
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error refreshing portfolio predictions: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Manually stores a prediction
    /// </summary>
    /// <param name="prediction">The prediction to store</param>
    /// <returns>The stored prediction with ID assigned</returns>
    [HttpPost]
    public async Task<ActionResult<Prediction>> StorePrediction([FromBody] Prediction prediction)
    {
        try
        {
            var storedPrediction = await modelPredictionService.StorePredictionAsync(prediction);
            return CreatedAtAction(nameof(GetLatestPrediction), 
                new { symbol = prediction.Symbol, modelSource = prediction.ModelSource }, 
                storedPrediction);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error storing prediction: {ex.Message}");
        }
    }
} 