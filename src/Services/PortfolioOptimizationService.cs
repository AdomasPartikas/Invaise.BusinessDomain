using AutoMapper;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for optimizing portfolios using Gaia
/// </summary>
public class PortfolioOptimizationService(
    IDatabaseService databaseService,
    InvaiseDbContext dbContext,
    IGaiaService gaiaService,
    Serilog.ILogger logger,
    IMapper mapper) : IPortfolioOptimizationService
{
    /// <inheritdoc />
    public async Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string userId, string portfolioId)
    {
        try
        {
            // Get the portfolio (default or specified)
            var portfolio = await dbContext.Portfolios
                    .Include(p => p.PortfolioStocks)
                    .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

            if (portfolio == null)
            {
                logger.Warning("Portfolio not found for user {UserId} with ID {PortfolioId}", userId, portfolioId ?? "default");
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = $"Portfolio not found for user {userId}",
                    Timestamp = DateTime.UtcNow.ToLocalTime()
                };
            }

            // Get symbols from the portfolio
            var symbols = portfolio.PortfolioStocks.Select(ps => ps.Symbol).ToList();

            if (symbols.Count == 0)
            {
                logger.Warning("No symbols found in portfolio {PortfolioId} for user {UserId}", portfolio.Id, userId);
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Explanation = "No symbols found in portfolio to optimize",
                    Timestamp = DateTime.UtcNow.ToLocalTime(),
                    Successful = false,
                    ErrorMessage = "No symbols found in portfolio to optimize"
                };
            }

            // Call Gaia service to optimize the portfolio
            var optimizationResult = await gaiaService.OptimizePortfolioAsync(portfolioId);

            // Store the optimization in the database
            var optimization = await StoreOptimizationResultAsync(portfolio.Id, optimizationResult);

            optimizationResult.Successful = true;
            optimizationResult.OptimizationId = optimization.Id;
            
            return optimizationResult;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error optimizing portfolio for user {UserId}", userId);
            return new PortfolioOptimizationResult
            {
                UserId = userId,
                Successful = false,
                ErrorMessage = $"Error optimizing portfolio: {ex.Message}",
                Timestamp = DateTime.UtcNow.ToLocalTime()
            };
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PortfolioOptimizationResult>> GetOptimizationHistoryAsync(
        string userId, string portfolioId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            // Get the portfolio (default or specified)
            var portfolio = await dbContext.Portfolios
                    .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

            if (portfolio == null)
            {
                logger.Warning("Portfolio not found for user {UserId} with ID {PortfolioId}", userId, portfolioId ?? "default");
                return new List<PortfolioOptimizationResult>
                {
                    new()
                    {
                        UserId = userId,
                        Successful = false,
                        ErrorMessage = $"Portfolio not found for user {userId}",
                        Timestamp = DateTime.UtcNow.ToLocalTime()
                    }
                };
            }

            // Define start and end dates if not specified
            var effectiveStartDate = startDate ?? DateTime.UtcNow.AddMonths(-1).ToLocalTime();
            var effectiveEndDate = endDate ?? DateTime.UtcNow.ToLocalTime();

            // Get optimizations from the database
            var optimizations = await dbContext.PortfolioOptimizations
                .Include(o => o.Recommendations)
                .Where(o => o.PortfolioId == portfolio.Id &&
                           o.Timestamp >= effectiveStartDate &&
                           o.Timestamp <= effectiveEndDate)
                .OrderByDescending(o => o.Timestamp)
                .ToListAsync();

            // Convert to result models
            var results = optimizations.Select(mapper.Map<PortfolioOptimizationResult>).ToList();
                        
            // Mark all results as successful
            foreach (var result in results)
            {
                result.Successful = true;
            }
            
            return results;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting optimization history for user {UserId}", userId);
            return new List<PortfolioOptimizationResult>
            {
                new()
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = $"Error getting optimization history: {ex.Message}",
                    Timestamp = DateTime.UtcNow.ToLocalTime()
                }
            };
        }
    }

    /// <inheritdoc />
    public async Task<PortfolioOptimizationResult> ApplyOptimizationRecommendationAsync(string userId, string optimizationId)
    {
        try
        {
            // Get the optimization
            var optimization = await dbContext.PortfolioOptimizations
                .Include(o => o.Recommendations)
                .FirstOrDefaultAsync(o => o.Id == optimizationId && o.UserId == userId);

            if (optimization == null)
            {
                logger.Warning("Optimization {OptimizationId} not found for user {UserId}", optimizationId, userId);
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = $"Optimization {optimizationId} not found for user {userId}"
                };
            }

            // Get the portfolio
            var portfolio = await dbContext.Portfolios
                .Include(p => p.PortfolioStocks)
                .FirstOrDefaultAsync(p => p.Id == optimization.PortfolioId && p.UserId == userId);

            if (portfolio == null)
            {
                logger.Warning("Portfolio {PortfolioId} not found for user {UserId}", optimization.PortfolioId, userId);
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = $"Portfolio {optimization.PortfolioId} not found for user {userId}"
                };
            }

            if (optimization.IsApplied)
            {
                logger.Warning("Optimization {OptimizationId} has already been applied", optimizationId);
                var result = mapper.Map<PortfolioOptimizationResult>(optimization);
                result.Successful = false;
                result.ErrorMessage = $"Optimization {optimizationId} has already been applied";
                return result;
            }

            // Apply the recommendations
            var existingStocks = portfolio.PortfolioStocks.ToDictionary(ps => ps.Symbol);

            foreach (var recommendation in optimization.Recommendations)
            {
                if (existingStocks.TryGetValue(recommendation.Symbol, out var portfolioStock))
                {
                    // Update existing stock
                    portfolioStock.Quantity = recommendation.TargetQuantity;
                    portfolioStock.LastUpdated = DateTime.UtcNow.ToLocalTime();
                }
                else if (recommendation.TargetQuantity > 0)
                {
                    // Add new stock
                    portfolio.PortfolioStocks.Add(new PortfolioStock
                    {
                        PortfolioId = portfolio.Id,
                        Portfolio = portfolio, // Set the required Portfolio property
                        Symbol = recommendation.Symbol,
                        Quantity = recommendation.TargetQuantity,
                        CurrentTotalValue = 0, // Will be updated by the portfolio service
                        TotalBaseValue = 0, // Will be updated by the portfolio service
                        PercentageChange = 0, // Will be updated by the portfolio service
                        LastUpdated = DateTime.UtcNow.ToLocalTime()
                    });
                }
            }

            // Update the portfolio
            portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();

            // Mark the optimization as applied
            optimization.IsApplied = true;
            optimization.AppliedDate = DateTime.UtcNow.ToLocalTime();

            await dbContext.SaveChangesAsync();
            var optimizationResult = mapper.Map<PortfolioOptimizationResult>(optimization);
            optimizationResult.Successful = true;
            return optimizationResult;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error applying optimization {OptimizationId} for user {UserId}", optimizationId, userId);
            return new PortfolioOptimizationResult
            {
                UserId = userId,
                Successful = false,
                ErrorMessage = $"Error applying optimization: {ex.Message}"
            };
        }
    }

    #region Private Helper Methods

    private async Task<PortfolioOptimization> StoreOptimizationResultAsync(string portfolioId, PortfolioOptimizationResult result)
    {
        var optimization = mapper.Map<PortfolioOptimization>(result);
        optimization.PortfolioId = portfolioId; // Set the portfolio ID
        optimization.ModelVersion = await gaiaService.GetModelVersionAsync();

        dbContext.PortfolioOptimizations.Add(optimization);
        await dbContext.SaveChangesAsync();

        return optimization;
    }

    #endregion
} 