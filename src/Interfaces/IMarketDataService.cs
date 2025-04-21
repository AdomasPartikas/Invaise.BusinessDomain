using Invaise.BusinessDomain.API.Models;
using Hangfire;

namespace Invaise.BusinessDomain.API.Interfaces;


/// <summary>
/// Provides methods for retrieving market data.
/// </summary>
/// <remarks>
/// This interface is used to fetch and import market data from external sources into the database.
/// It includes methods for fetching historical market data, importing company data, and importing daily market data.
/// </remarks>
[DisableConcurrentExecution(timeoutInSeconds: 0)]
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public interface IMarketDataService
{
    /// <summary>
    /// Fetches historical market data for all symbols and imports it into the database.
    /// </summary>
    Task FetchAndImportHistoricalMarketDataAsync();

    /// <summary>
    /// Imports company data into the database.
    /// </summary>
    Task ImportCompanyDataAsync();

    /// <summary>
    /// Imports daily market data into the database.
    /// </summary>
    Task ImportIntradayMarketDataAsync();
}