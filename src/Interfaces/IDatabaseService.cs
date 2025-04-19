using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Represents a service for interacting with the database
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Retrieves all unique market data symbols from the database asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an enumerable collection of unique market data symbols.</returns>
    Task<IEnumerable<string>> GetAllUniqueMarketDataSymbolsAsync();

    /// <summary>
    /// Retrieves market data for a specific symbol within an optional date range asynchronously.
    /// </summary>
    /// <param name="symbol">The market data symbol to retrieve data for.</param>
    /// <param name="start">The optional start date for the data range. If null, no start date filter is applied.</param>
    /// <param name="end">The optional end date for the data range. If null, no end date filter is applied.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an enumerable collection of market data for the specified symbol and date range.</returns>
    Task<IEnumerable<MarketData>> GetMarketDataAsync(string symbol, DateTime? start, DateTime? end);
}