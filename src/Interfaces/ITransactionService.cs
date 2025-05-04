using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Hangfire;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for managing transactions
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 0)]
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public interface ITransactionService
{
    /// <summary>
    /// Creates a transaction from a portfolio optimization recommendation
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="currentQuantity">Current quantity of the stock</param>
    /// <param name="targetQuantity">Target quantity of the stock</param>
    /// <returns>The created transaction</returns>
    Task<Transaction> CreateTransactionFromRecommendationAsync(
        string userId, 
        string portfolioId, 
        string symbol, 
        decimal currentQuantity, 
        decimal targetQuantity);
    
    /// <summary>
    /// Processes pending transactions
    /// </summary>
    /// <returns>Number of transactions processed</returns>
    Task<int> ProcessPendingTransactionsAsync();
    
    /// <summary>
    /// Checks if the transaction can be processed immediately
    /// </summary>
    /// <param name="transaction">The transaction to check</param>
    /// <returns>True if the transaction can be processed immediately, false otherwise</returns>
    Task<bool> CanProcessImmediatelyAsync(Transaction transaction);
    
    /// <summary>
    /// Processes a specific transaction
    /// </summary>
    /// <param name="transaction">The transaction to process</param>
    /// <returns>True if processing was successful, false otherwise</returns>
    Task<bool> ProcessTransactionAsync(Transaction transaction);
    
    /// <summary>
    /// Applies a transaction to the portfolio
    /// </summary>
    /// <param name="transaction">The transaction to apply</param>
    /// <returns>True if transaction was applied successfully, false otherwise</returns>
    Task<bool> ApplyTransactionToPortfolioAsync(Transaction transaction);
    
    /// <summary>
    /// Gets all pending transactions
    /// </summary>
    /// <returns>List of pending transactions</returns>
    Task<IEnumerable<Transaction>> GetPendingTransactionsAsync();
    
    /// <summary>
    /// Gets current price for a symbol
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Current price if available, null otherwise</returns>
    Task<decimal?> GetCurrentPriceAsync(string symbol);
} 