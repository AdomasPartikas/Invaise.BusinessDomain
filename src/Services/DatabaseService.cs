using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Provides methods for interacting with the database to retrieve market data.
/// </summary>
/// <remarks>
/// This service is responsible for querying the database for market data and symbols.
/// It includes methods to retrieve unique market data symbols and filter market data
/// based on specific criteria such as symbol, start date, and end date.
/// </remarks>
public class DatabaseService(InvaiseDbContext context) : IDatabaseService
{
    /// <summary>
    /// Retrieves all unique market data symbols from the database.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains 
    /// an enumerable collection of unique market data symbols as strings.
    /// </returns>
    /// <remarks>
    /// This method queries the database for distinct symbols in the MarketData table.
    /// </remarks>
    public async Task<IEnumerable<string>> GetAllUniqueMarketDataSymbolsAsync()
    {
        var symbols = await context.HistoricalMarketData
            .Select(m => m.Symbol)
            .Distinct()
            .ToListAsync();

        return symbols;
    }

    public async Task<IEnumerable<IntradayMarketData>> GetIntradayMarketDataAsync(string symbol, DateTime? start, DateTime? end)
    {
        var query = context.IntradayMarketData.AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(m => m.Symbol == symbol);

        if (start.HasValue)
            query = query.Where(m => m.Timestamp >= start.Value);

        if (end.HasValue)
            query = query.Where(m => m.Timestamp <= end.Value);

        return await query.OrderBy(m => m.Timestamp).ToListAsync();
    }

    /// <summary>
    /// Retrieves a collection of market data filtered by the specified criteria.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data by. If null or empty, no filtering is applied for the symbol.</param>
    /// <param name="start">The start date to filter the market data. If null, no filtering is applied for the start date.</param>
    /// <param name="end">The end date to filter the market data. If null, no filtering is applied for the end date.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an 
    /// <see cref="IEnumerable{MarketData}"/> of market data ordered by date.
    /// </returns>
    public async Task<IEnumerable<HistoricalMarketData>> GetHistoricalMarketDataAsync(string symbol, DateTime? start, DateTime? end)
    {
        var query = context.HistoricalMarketData.AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(m => m.Symbol == symbol);

        if (start.HasValue)
            query = query.Where(m => m.Date >= start.Value);

        if (end.HasValue)
            query = query.Where(m => m.Date <= end.Value);

        return await query.OrderBy(m => m.Date).ToListAsync();
    }
}