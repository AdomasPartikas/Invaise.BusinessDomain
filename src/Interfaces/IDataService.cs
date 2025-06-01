using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Provides methods for retrieving market data.
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Cleans and transforms the raw S&amp;P dataset into a structured format suitable for analysis.
    /// Processes ticker data by date and flattens multi-dimensional data into individual records per symbol.
    /// </summary>
    /// <returns>A task that represents the asynchronous dataset cleanup operation</returns>
    Task SMPDatasetCleanupAsync();
}