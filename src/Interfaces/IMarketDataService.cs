using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Provides methods for retrieving market data.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Fetches historical market data for all symbols and imports it into the database.
    /// </summary>
    Task FetchAndImportMarketDataAsync();
}