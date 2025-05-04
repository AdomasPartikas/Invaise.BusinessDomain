using AutoMapper;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
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
    ITransactionService transactionService,
    Serilog.ILogger logger,
    IMapper mapper) : IPortfolioOptimizationService
{
    /// <inheritdoc />
    public async Task<bool> HasOngoingOptimizationAsync(string userId, string portfolioId)
    {
        return await dbContext.PortfolioOptimizations
            .AnyAsync(o => o.UserId == userId && 
                          o.PortfolioId == portfolioId && 
                          (o.Status == PortfolioOptimizationStatus.Created || 
                           o.Status == PortfolioOptimizationStatus.InProgress));
    }

    public async Task<TimeSpan> GetRemainingCoolOffTime(string userId, string portfolioId)
    {
        var optimization = await dbContext.PortfolioOptimizations
            .Where(o => o.UserId == userId && o.PortfolioId == portfolioId)
            .OrderByDescending(o => o.Timestamp)
            .FirstOrDefaultAsync();

        if (optimization == null)
        {
            return TimeSpan.Zero; // No optimizations found
        }

        if (optimization.Status != PortfolioOptimizationStatus.Applied)
        {
            return TimeSpan.Zero; // No cool-off time if not in Created status
        }

        var coolOffPeriod = TimeSpan.FromHours(GaiaConstants.OptimizationCoolOffPeriod);
        var elapsedTime = DateTime.UtcNow.ToLocalTime() - optimization.AppliedDate!.Value.ToLocalTime();
        var remainingTime = coolOffPeriod - elapsedTime;

        return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero; // Return zero if negative
    }

    /// <inheritdoc />
    public async Task<PortfolioOptimizationStatus> GetOptimizationStatusAsync(string userId, string optimizationId)
    {
        var optimization = await dbContext.PortfolioOptimizations
            .FirstOrDefaultAsync(o => o.Id == optimizationId && o.UserId == userId);

        if (optimization == null)
        {
            throw new KeyNotFoundException($"Optimization {optimizationId} not found for user {userId}");
        }

        return optimization.Status;
    }

    /// <inheritdoc />
    public async Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string userId, string portfolioId)
    {
        try
        {
            // Check for existing Created optimization for this portfolio
            var existingOptimization = await dbContext.PortfolioOptimizations
                .Include(o => o.Recommendations)
                .FirstOrDefaultAsync(o => o.UserId == userId && 
                                        o.PortfolioId == portfolioId && 
                                        o.Status == PortfolioOptimizationStatus.Created);
            
            if (existingOptimization != null)
            {
                logger.Information("Returning existing optimization {OptimizationId} for portfolio {PortfolioId}", 
                    existingOptimization.Id, portfolioId);
                
                // Map and return the existing optimization
                var result = mapper.Map<PortfolioOptimizationResult>(existingOptimization);
                result.Successful = true;
                return result;
            }
            
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
                    Timestamp = DateTime.UtcNow.ToLocalTime(),
                    Status = PortfolioOptimizationStatus.Failed
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
                    ErrorMessage = "No symbols found in portfolio to optimize",
                    Status = PortfolioOptimizationStatus.Failed
                };
            }
            
            // Create an initial optimization record with Created status
            var initialOptimization = new PortfolioOptimization
            {
                UserId = userId,
                PortfolioId = portfolio.Id,
                Status = PortfolioOptimizationStatus.Created,
                ModelVersion = await gaiaService.GetModelVersionAsync()
            };
            
            dbContext.PortfolioOptimizations.Add(initialOptimization);
            await dbContext.SaveChangesAsync();
            
            try
            {
                // Call Gaia service to optimize the portfolio
                var optimizationResult = await gaiaService.OptimizePortfolioAsync(portfolioId);
                
                // Update the optimization with the results
                initialOptimization.Status = PortfolioOptimizationStatus.Created;
                initialOptimization.Explanation = optimizationResult.Explanation;
                initialOptimization.Confidence = optimizationResult.Confidence;
                initialOptimization.SharpeRatio = optimizationResult.Metrics.SharpeRatio;
                initialOptimization.MeanReturn = optimizationResult.Metrics.MeanReturn;
                initialOptimization.Variance = optimizationResult.Metrics.Variance;
                initialOptimization.ExpectedReturn = optimizationResult.Metrics.ExpectedReturn;
                initialOptimization.ProjectedSharpeRatio = optimizationResult.Metrics.ProjectedSharpeRatio;
                initialOptimization.ProjectedMeanReturn = optimizationResult.Metrics.ProjectedMeanReturn;
                initialOptimization.ProjectedVariance = optimizationResult.Metrics.ProjectedVariance;
                initialOptimization.ProjectedExpectedReturn = optimizationResult.Metrics.ProjectedExpectedReturn;
                
                // Add recommendations
                foreach (var rec in optimizationResult.Recommendations)
                {
                    initialOptimization.Recommendations.Add(new PortfolioOptimizationRecommendation
                    {
                        OptimizationId = initialOptimization.Id,
                        Symbol = rec.Symbol,
                        Action = rec.Action,
                        CurrentQuantity = rec.CurrentQuantity,
                        TargetQuantity = rec.TargetQuantity,
                        CurrentWeight = rec.CurrentWeight,
                        TargetWeight = rec.TargetWeight,
                        Explanation = rec.Explanation
                    });
                }
                
                await dbContext.SaveChangesAsync();
                
                // Map the result properly using AutoMapper
                var result = mapper.Map<PortfolioOptimizationResult>(initialOptimization);
                result.Successful = true;
                
                return result;
            }
            catch (Exception ex)
            {
                // If optimization fails, update the status to Failed
                initialOptimization.Status = PortfolioOptimizationStatus.Failed;
                initialOptimization.Explanation = $"Optimization failed: {ex.Message}";
                await dbContext.SaveChangesAsync();
                
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error optimizing portfolio for user {UserId}", userId);
            return new PortfolioOptimizationResult
            {
                UserId = userId,
                Successful = false,
                ErrorMessage = $"Error optimizing portfolio: {ex.Message}",
                Timestamp = DateTime.UtcNow.ToLocalTime(),
                Status = PortfolioOptimizationStatus.Failed
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelExistingOptimizationsForSymbolsAsync(IEnumerable<string> symbols)
    {
        try
        {
            // Get all 'Created' optimizations that might be affected by new heat predictions
            var portfolioIds = await dbContext.PortfolioStocks
                .Where(ps => symbols.Contains(ps.Symbol))
                .Select(ps => ps.PortfolioId)
                .Distinct()
                .ToListAsync();
            
            if (!portfolioIds.Any())
            {
                return true; // No portfolios affected
            }
            
            // Find all created optimizations for these portfolios
            var optimizations = await dbContext.PortfolioOptimizations
                .Where(o => portfolioIds.Contains(o.PortfolioId) && 
                           o.Status == PortfolioOptimizationStatus.Created)
                .ToListAsync();
            
            if (!optimizations.Any())
            {
                return true; // No optimizations to cancel
            }
            
            // Cancel each optimization
            foreach (var optimization in optimizations)
            {
                optimization.Status = PortfolioOptimizationStatus.Canceled;
                optimization.Explanation += " (Canceled due to new heat predictions)";
            }
            
            await dbContext.SaveChangesAsync();
            logger.Information("Canceled {Count} optimizations due to new heat predictions", optimizations.Count);
            
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error canceling existing optimizations for symbols");
            return false;
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
                        Timestamp = DateTime.UtcNow.ToLocalTime(),
                        Status = PortfolioOptimizationStatus.Failed
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
                        
            // Mark all results as successful except for failed ones
            foreach (var result in results)
            {
                result.Successful = result.Status != PortfolioOptimizationStatus.Failed;
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
                    Timestamp = DateTime.UtcNow.ToLocalTime(),
                    Status = PortfolioOptimizationStatus.Failed
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
                    ErrorMessage = $"Optimization {optimizationId} not found for user {userId}",
                    Status = PortfolioOptimizationStatus.Failed
                };
            }

            // Verify optimization status
            if (optimization.Status != PortfolioOptimizationStatus.Created)
            {
                logger.Warning("Cannot apply optimization {OptimizationId} with status {Status}", optimizationId, optimization.Status);
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = $"Cannot apply optimization with status {optimization.Status}",
                    Status = optimization.Status
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
                    ErrorMessage = $"Portfolio {optimization.PortfolioId} not found for user {userId}",
                    Status = PortfolioOptimizationStatus.Failed
                };
            }

            // Update status to InProgress while applying changes
            optimization.Status = PortfolioOptimizationStatus.InProgress;
            await dbContext.SaveChangesAsync();
            
            try
            {
                // Create a dictionary of existing portfolio stocks for quick lookup
                var existingStocks = portfolio.PortfolioStocks.ToDictionary(ps => ps.Symbol);
                
                // Store transaction IDs for potential rollback
                var transactionIdList = new List<string>();
                
                // Process each recommendation using the transaction service
                foreach (var recommendation in optimization.Recommendations)
                {
                    decimal currentQuantity = 0;
                    if (existingStocks.TryGetValue(recommendation.Symbol, out var portfolioStock))
                    {
                        currentQuantity = portfolioStock.Quantity;
                    }
                    
                    // Only create transactions if there's an actual change in quantity
                    if (recommendation.TargetQuantity != currentQuantity)
                    {
                        var transaction = await transactionService.CreateTransactionFromRecommendationAsync(
                            userId,
                            portfolio.Id,
                            recommendation.Symbol,
                            currentQuantity,
                            recommendation.TargetQuantity);
                        
                        if (transaction != null)
                        {
                            transactionIdList.Add(transaction.Id);
                            // Add to the optimization's TransactionIds collection
                            optimization.TransactionIds.Add(transaction.Id);
                        }
                    }
                }
                
                // Store transaction info in explanation but don't rely on it for retrieval
                if (transactionIdList.Any())
                {
                    optimization.Explanation += $" Applied with {transactionIdList.Count} transactions created";
                }
                
                // Flag as applied but DON'T change status to Applied yet
                // Status will remain InProgress until we confirm all transactions have completed
                optimization.IsApplied = true;
                await dbContext.SaveChangesAsync();
                
                // Don't run background task, rely on periodic service instead
                // Remove: _ = Task.Run(() => CheckAndUpdateTransactionStatusAsync(optimization.Id, transactionIdList));
                
                var optimizationResult = mapper.Map<PortfolioOptimizationResult>(optimization);
                optimizationResult.Successful = true;
                return optimizationResult;
            }
            catch (Exception ex)
            {
                // If applying fails, update the status back to Created
                optimization.Status = PortfolioOptimizationStatus.Created;
                optimization.Explanation += $" (Failed to apply: {ex.Message})";
                await dbContext.SaveChangesAsync();
                
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error applying optimization {OptimizationId} for user {UserId}", optimizationId, userId);
            return new PortfolioOptimizationResult
            {
                UserId = userId,
                Successful = false,
                ErrorMessage = $"Error applying optimization: {ex.Message}",
                Status = PortfolioOptimizationStatus.Failed
            };
        }
    }

    /// <inheritdoc />
    public async Task<PortfolioOptimizationResult> CancelOptimizationAsync(string userId, string optimizationId)
    {
        try
        {
            // Get the optimization
            var optimization = await dbContext.PortfolioOptimizations
                .FirstOrDefaultAsync(o => o.Id == optimizationId && o.UserId == userId);

            if (optimization == null)
            {
                logger.Warning("Optimization {OptimizationId} not found for user {UserId}", optimizationId, userId);
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = "Optimization not found",
                    Status = PortfolioOptimizationStatus.Failed
                };
            }

            // Only allow cancellation of in-progress or created optimizations
            if (optimization.Status != PortfolioOptimizationStatus.InProgress && 
                optimization.Status != PortfolioOptimizationStatus.Created)
            {
                logger.Warning("Cannot cancel optimization {OptimizationId} with status {Status}", optimizationId, optimization.Status);
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Successful = false,
                    ErrorMessage = $"Cannot cancel optimization with status {optimization.Status}",
                    Status = optimization.Status
                };
            }

            // Check for related transactions that need to be canceled
            if (optimization.IsApplied)
            {
                // Use the TransactionIds collection instead of extracting from explanation
                if (optimization.TransactionIds.Any())
                {
                    foreach (var transactionId in optimization.TransactionIds)
                    {
                        // Cancel each transaction
                        try
                        {
                            await databaseService.CancelTransactionAsync(transactionId);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, "Failed to cancel transaction {TransactionId} for optimization {OptimizationId}", 
                                transactionId, optimizationId);
                        }
                    }
                }
            }

            // Mark the optimization as canceled
            optimization.Status = PortfolioOptimizationStatus.Canceled;
            optimization.Explanation += " (Canceled by user)";
            await dbContext.SaveChangesAsync();
            
            var result = mapper.Map<PortfolioOptimizationResult>(optimization);
            result.Successful = true;
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error canceling optimization {OptimizationId} for user {UserId}", optimizationId, userId);
            return new PortfolioOptimizationResult
            {
                UserId = userId,
                Successful = false,
                ErrorMessage = $"Error canceling optimization: {ex.Message}",
                Status = PortfolioOptimizationStatus.Failed
            };
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PortfolioOptimizationResult>> GetOptimizationsByPortfolioAsync(string userId, string portfolioId)
    {
        try
        {
            // Get all optimizations for this portfolio
            var optimizations = await dbContext.PortfolioOptimizations
                .Include(o => o.Recommendations)
                .Where(o => o.PortfolioId == portfolioId && o.UserId == userId)
                .OrderByDescending(o => o.Timestamp)
                .ToListAsync();

            // Convert to result models
            var results = optimizations.Select(mapper.Map<PortfolioOptimizationResult>).ToList();
                        
            // Mark all results as successful except for failed ones
            foreach (var result in results)
            {
                result.Successful = result.Status != PortfolioOptimizationStatus.Failed;
            }
            
            return results;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting optimizations for portfolio {PortfolioId} for user {UserId}", portfolioId, userId);
            return new List<PortfolioOptimizationResult>();
        }
    }

    public async Task EnsureCompletionOfAllInProgressOptimizationsAsync()
    {
        try
        {
            // Get all in-progress optimizations
            var inProgressOptimizations = await dbContext.PortfolioOptimizations
                .Where(o => o.Status == PortfolioOptimizationStatus.InProgress)
                .ToListAsync();

            foreach (var optimization in inProgressOptimizations)
            {
                // Check and update transaction status
                await CheckAndUpdateTransactionStatusAsync(optimization);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error ensuring completion of all in-progress optimizations");
        }
    }

    /// <summary>
    /// Checks the status of transactions and updates the optimization status accordingly
    /// </summary>
    /// <param name="optimization">The optimization</param>
    private async Task CheckAndUpdateTransactionStatusAsync(PortfolioOptimization optimization)
    {
        try
        {
            if (optimization == null)
                return;

            // Check all transactions
            bool allSucceeded = true;
            bool anyFailed = false;

            foreach (var transactionId in optimization.TransactionIds)
            {
                var transaction = await databaseService.GetTransactionByIdAsync(transactionId);
                if (transaction == null)
                {
                    continue;
                }

                if (transaction.Status == TransactionStatus.Failed)
                {
                    anyFailed = true;
                    break;
                }

                if (transaction.Status != TransactionStatus.Succeeded)
                {
                    allSucceeded = false;
                }
            }

            // Update optimization status based on transaction status
            if (anyFailed)
            {
                // At least one transaction failed, mark optimization as failed
                optimization.Status = PortfolioOptimizationStatus.Failed;
                optimization.Explanation += " (Failed due to failed transaction)";
                await dbContext.SaveChangesAsync();
                return;
            }

            if (allSucceeded)
            {
                // All transactions succeeded, mark optimization as applied
                optimization.Status = PortfolioOptimizationStatus.Applied;
                optimization.AppliedDate = DateTime.UtcNow.ToLocalTime();
                await dbContext.SaveChangesAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error checking transaction status for optimization {OptimizationId}", optimization.Id);
        }
    }
} 