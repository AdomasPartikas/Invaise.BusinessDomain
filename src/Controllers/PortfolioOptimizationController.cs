using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.AspNetCore.Mvc;
using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioOptimizationController(IPortfolioOptimizationService portfolioOptimizationService, Serilog.ILogger logger) : ControllerBase
{
    /// <summary>
    /// Optimizes a user's portfolio based on predictions from Gaia
    /// </summary>
    /// <param name="portfolioId">Optional portfolio ID (uses default portfolio if not specified)</param>
    /// <param name="riskTolerance">Optional risk tolerance factor (0-1)</param>
    /// <returns>Portfolio optimization results</returns>
    [HttpGet("optimize")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioOptimizationResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OptimizePortfolio([FromQuery] string? portfolioId = null)
    {
        try
        {
            var currentUser = (User)HttpContext.Items["User"]!;
            var userId = currentUser.Id;

            var result = await portfolioOptimizationService.OptimizePortfolioAsync(userId, portfolioId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            logger.Warning(ex, "Portfolio not found during optimization attempt");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error optimizing portfolio");
            return BadRequest($"Error optimizing portfolio: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the optimization history for a user's portfolio
    /// </summary>
    /// <param name="portfolioId">Optional portfolio ID (uses default portfolio if not specified)</param>
    /// <param name="startDate">Optional start date for history</param>
    /// <param name="endDate">Optional end date for history</param>
    /// <returns>List of historical optimization results</returns>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PortfolioOptimizationResult>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOptimizationHistory(
        [FromQuery] string? portfolioId = null, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var currentUser = (User)HttpContext.Items["User"]!;
            var userId = currentUser.Id;

            var results = await portfolioOptimizationService.GetOptimizationHistoryAsync(userId, portfolioId, startDate, endDate);
            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting optimization history");
            return BadRequest($"Error getting optimization history: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies optimization recommendations to a portfolio
    /// </summary>
    /// <param name="optimizationId">The optimization ID to apply</param>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>Success status</returns>
    [HttpPost("apply/{optimizationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyOptimizationRecommendation(string optimizationId)
    {
        try
        {
            var currentUser = (User)HttpContext.Items["User"]!;
            var userId = currentUser.Id;

            if (string.IsNullOrEmpty(optimizationId))
            {
                return BadRequest("Optimization ID is required");
            }

            var result = await portfolioOptimizationService.ApplyOptimizationRecommendationAsync(userId, optimizationId);
            
            if (!result.Successful)
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Optimization recommendation applied successfully" });
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error applying optimization");
            return BadRequest($"Error applying optimization: {ex.Message}");
        }
    }
} 