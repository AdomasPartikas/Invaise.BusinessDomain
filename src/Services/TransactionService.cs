using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling transactions
/// </summary>
public class TransactionService(
    InvaiseDbContext dbContext, 
    IDatabaseService databaseService, 
    IMarketDataService marketDataService,
    Serilog.ILogger logger) : ITransactionService
{
    /// <inheritdoc />
    public async Task<Transaction> CreateTransactionFromRecommendationAsync(
        string userId, 
        string portfolioId, 
        string symbol, 
        decimal currentQuantity, 
        decimal targetQuantity)
    {
        var transactionType = targetQuantity > currentQuantity 
            ? TransactionType.Buy 
            : TransactionType.Sell;
        
        var quantity = Math.Abs(targetQuantity - currentQuantity);
        
        // Get current price for the symbol
        var currentPrice = await GetCurrentPriceAsync(symbol);
        if (currentPrice == null)
        {
            logger.Warning("Could not get current price for symbol: {Symbol}", symbol);
            throw new InvalidOperationException($"Could not get current price for symbol: {symbol}");
        }
        
        var transaction = new Transaction
        {
            UserId = userId,
            PortfolioId = portfolioId,
            Symbol = symbol,
            Quantity = quantity,
            PricePerShare = currentPrice.Value,
            TransactionValue = quantity * currentPrice.Value,
            TransactionDate = DateTime.UtcNow.ToLocalTime(),
            Type = transactionType,
            TriggeredBy = AvailableTransactionTriggers.AI,
            Status = TransactionStatus.OnHold
        };
        
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();
        
        // Process immediately if possible
        if (await CanProcessImmediatelyAsync(transaction))
        {
            await ProcessTransactionAsync(transaction);
        }
        
        return transaction;
    }

    /// <inheritdoc />
    public async Task<int> ProcessPendingTransactionsAsync()
    {
        var pendingTransactions = await GetPendingTransactionsAsync();
        int processed = 0;
        
        foreach (var transaction in pendingTransactions)
        {
            if (await ProcessTransactionAsync(transaction))
            {
                processed++;
            }
        }
        
        return processed;
    }

    /// <inheritdoc />
    public async Task<bool> CanProcessImmediatelyAsync(Transaction transaction)
    {
        // We can only process transactions if the market is open
        return await marketDataService.IsMarketOpenAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ProcessTransactionAsync(Transaction transaction)
    {
        try
        {
            // If market is closed, keep the transaction on hold
            if (!await marketDataService.IsMarketOpenAsync())
            {
                logger.Debug("Market is closed. Transaction {TransactionId} remains on hold", transaction.Id);
                return false;
            }
            
            // Apply the transaction to the portfolio
            bool result = await ApplyTransactionToPortfolioAsync(transaction);
            
            // Update transaction status
            transaction.Status = result ? TransactionStatus.Succeeded : TransactionStatus.Failed;
            await dbContext.SaveChangesAsync();
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error processing transaction {TransactionId}", transaction.Id);
            transaction.Status = TransactionStatus.Failed;
            await dbContext.SaveChangesAsync();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ApplyTransactionToPortfolioAsync(Transaction transaction)
    {
        try
        {
            // Get current price for the symbol
            var currentPrice = await GetCurrentPriceAsync(transaction.Symbol);
            if (currentPrice == null)
            {
                logger.Warning("Could not get current price for symbol: {Symbol}", transaction.Symbol);
                return false;
            }
            
            // Get the portfolio stock
            var portfolioStock = await dbContext.PortfolioStocks
                .FirstOrDefaultAsync(ps => ps.PortfolioId == transaction.PortfolioId && ps.Symbol == transaction.Symbol);
            
            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    if (portfolioStock == null)
                    {
                        // Create a new portfolio stock if it doesn't exist
                        portfolioStock = new PortfolioStock
                        {
                            PortfolioId = transaction.PortfolioId,
                            Symbol = transaction.Symbol,
                            Quantity = transaction.Quantity,
                            CurrentTotalValue = transaction.Quantity * currentPrice.Value,
                            TotalBaseValue = transaction.TransactionValue,
                            PercentageChange = 0, // It's new, so no change yet
                            LastUpdated = DateTime.UtcNow.ToLocalTime(),
                            Portfolio = await dbContext.Portfolios.FindAsync(transaction.PortfolioId) // Set the required Portfolio property
                        };
                        
                        dbContext.PortfolioStocks.Add(portfolioStock);
                    }
                    else
                    {
                        // Update the existing portfolio stock
                        var newQuantity = portfolioStock.Quantity + transaction.Quantity;
                        var newTotalBaseValue = portfolioStock.TotalBaseValue + transaction.TransactionValue;
                        
                        portfolioStock.Quantity = newQuantity;
                        portfolioStock.TotalBaseValue = newTotalBaseValue;
                        portfolioStock.CurrentTotalValue = newQuantity * currentPrice.Value;
                        portfolioStock.PercentageChange = ((portfolioStock.CurrentTotalValue - portfolioStock.TotalBaseValue) / portfolioStock.TotalBaseValue) * 100;
                        portfolioStock.LastUpdated = DateTime.UtcNow.ToLocalTime();
                    }
                    break;
                    
                case TransactionType.Sell:
                    if (portfolioStock == null)
                    {
                        logger.Warning("Cannot sell stock {Symbol} that doesn't exist in portfolio {PortfolioId}", 
                            transaction.Symbol, transaction.PortfolioId);
                        return false;
                    }
                    
                    // Ensure we're not selling more than we have
                    if (transaction.Quantity > portfolioStock.Quantity)
                    {
                        logger.Warning("Cannot sell {Quantity} shares of {Symbol}, only {Available} available", 
                            transaction.Quantity, transaction.Symbol, portfolioStock.Quantity);
                        return false;
                    }
                    
                    // Calculate the percentage of the position being sold
                    var sellRatio = transaction.Quantity / portfolioStock.Quantity;
                    var baseValueReduction = portfolioStock.TotalBaseValue * sellRatio;
                    
                    // Update the portfolio stock
                    portfolioStock.Quantity -= transaction.Quantity;
                    portfolioStock.TotalBaseValue -= baseValueReduction;
                    
                    // If all shares were sold, remove the stock from the portfolio
                    if (portfolioStock.Quantity <= 0)
                    {
                        dbContext.PortfolioStocks.Remove(portfolioStock);
                    }
                    else
                    {
                        // Update values for the remaining shares
                        portfolioStock.CurrentTotalValue = portfolioStock.Quantity * currentPrice.Value;
                        portfolioStock.PercentageChange = ((portfolioStock.CurrentTotalValue - portfolioStock.TotalBaseValue) / portfolioStock.TotalBaseValue) * 100;
                        portfolioStock.LastUpdated = DateTime.UtcNow.ToLocalTime();
                    }
                    break;
            }
            
            // Update the portfolio's LastUpdated timestamp
            var portfolio = await dbContext.Portfolios.FindAsync(transaction.PortfolioId);
            if (portfolio != null)
            {
                portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();
            }
            
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error applying transaction {TransactionId} to portfolio", transaction.Id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync()
    {
        return await dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.OnHold)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<decimal?> GetCurrentPriceAsync(string symbol)
    {
        // First try to get intraday data (most current)
        var intradayData = await databaseService.GetLatestIntradayMarketDataAsync(symbol);
        if (intradayData != null)
        {
            return intradayData.Current;
        }
        
        // Fall back to historical data if no intraday data is available
        var historicalData = await databaseService.GetLatestHistoricalMarketDataAsync(symbol);
        if (historicalData != null)
        {
            return historicalData.Close;
        }
        
        // No price data available
        return null;
    }
} 