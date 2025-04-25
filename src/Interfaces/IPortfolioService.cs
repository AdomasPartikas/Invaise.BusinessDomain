using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for managing portfolios and their stocks
/// </summary>
public interface IPortfolioService
{
    Task RefreshAllPortfoliosAsync();
} 