using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Provides methods for retrieving market data.
/// </summary>
public interface IDataService
{
    Task SMPDatasetCleanupAsync();
}