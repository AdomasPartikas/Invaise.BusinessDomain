using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.AspNetCore.Mvc;
using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for portfolio optimization operations.
/// </summary>
/// <param name="portfolioOptimizationService">Service for portfolio optimization operations.</param>
/// <param name="logger">Logger instance for logging.</param>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioOptimizationController(IPortfolioOptimizationService portfolioOptimizationService, Serilog.ILogger logger) : ControllerBase
{
    /// <summary>
    /// Optimizes a user's portfolio based on predictions from Gaia
    /// </summary>
    /// <param name="portfolioId">Optional portfolio ID (uses default portfolio if not specified)</param>
    /// <returns>Portfolio optimization results</returns>
    [HttpGet("optimize")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioOptimizationResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OptimizePortfolio([FromQuery] string portfolioId)
    {
        try
        {
            var currentUser = (User)HttpContext.Items["User"]!;
            var userId = currentUser.Id;

            // Check if there's an ongoing optimization for this portfolio
            var hasOngoingOptimization = await portfolioOptimizationService.HasOngoingOptimizationAsync(userId, portfolioId);
            if (hasOngoingOptimization)
            {
                // Get the status of the current optimization to provide more detailed information
                var existingOptimizations = await portfolioOptimizationService.GetOptimizationsByPortfolioAsync(userId, portfolioId);
                var inProgressOpt = existingOptimizations.FirstOrDefault(o => 
                    o.Status == PortfolioOptimizationStatus.InProgress || 
                    o.Status == PortfolioOptimizationStatus.Created);
                
                if (inProgressOpt != null)
                {
                    var statusInfo = inProgressOpt.Status == PortfolioOptimizationStatus.InProgress 
                        ? "in progress with transactions being applied" 
                        : "ready to be applied";
                    
                    logger.Warning("Attempted to optimize portfolio {PortfolioId} for user {UserId} while an optimization is already {Status}", 
                        portfolioId, userId, statusInfo);
                    
                    return Conflict($"There is already an optimization {statusInfo} for this portfolio. " +
                        $"Please wait for it to complete, apply it, or cancel it first (Optimization ID: {inProgressOpt.OptimizationId})");
                }
                
                logger.Warning("Attempted to optimize portfolio {PortfolioId} for user {UserId} while an optimization is already in progress", 
                    portfolioId, userId);
                return Conflict("There is already an optimization in progress for this portfolio. Please wait for it to complete or cancel it.");
            }

            var coolOffPeriod = await portfolioOptimizationService.GetRemainingCoolOffTime(userId, portfolioId);
            if (coolOffPeriod > TimeSpan.Zero)
            {
                logger.Warning("Attempted to optimize portfolio {PortfolioId} for user {UserId} during cool-off period", 
                    portfolioId, userId);
                return Conflict($"It is advised that you wait {Math.Floor(coolOffPeriod.TotalHours)} Hours and {coolOffPeriod.Minutes} Minutes before optimizing this portfolio again.");
            }

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
        [FromQuery] string portfolioId, 
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
    /// <returns>Success status</returns>
    [HttpPost("apply/{optimizationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

            // Check if the optimization is in a valid state for applying
            var optimizationStatus = await portfolioOptimizationService.GetOptimizationStatusAsync(userId, optimizationId);
            if (optimizationStatus == PortfolioOptimizationStatus.InProgress)
            {
                return Conflict("Cannot apply an optimization that is still in progress");
            }
            else if (optimizationStatus == PortfolioOptimizationStatus.Applied)
            {
                return Conflict("This optimization has already been applied");
            }
            else if (optimizationStatus == PortfolioOptimizationStatus.Canceled)
            {
                return BadRequest("Cannot apply a canceled optimization");
            }
            else if (optimizationStatus == PortfolioOptimizationStatus.Failed)
            {
                return BadRequest("Cannot apply a failed optimization");
            }

            var result = await portfolioOptimizationService.ApplyOptimizationRecommendationAsync(userId, optimizationId);
            
            if (!result.Successful)
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Optimization recommendation applied successfully. Transactions are being processed." });
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error applying optimization");
            return BadRequest($"Error applying optimization: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels an in-progress optimization
    /// </summary>
    /// <param name="optimizationId">The optimization ID to cancel</param>
    /// <returns>Success status</returns>
    [HttpPost("cancel/{optimizationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOptimization(string optimizationId)
    {
        try
        {
            var currentUser = (User)HttpContext.Items["User"]!;
            var userId = currentUser.Id;

            if (string.IsNullOrEmpty(optimizationId))
            {
                return BadRequest("Optimization ID is required");
            }

            var result = await portfolioOptimizationService.CancelOptimizationAsync(userId, optimizationId);
            
            if (!result.Successful)
            {
                if (result.ErrorMessage == "Optimization not found")
                {
                    return NotFound(new { message = result.ErrorMessage });
                }
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Optimization canceled successfully" });
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error canceling optimization");
            return BadRequest($"Error canceling optimization: {ex.Message}");
        }
    }
} 