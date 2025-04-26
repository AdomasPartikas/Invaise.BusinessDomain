using Invaise.BusinessDomain.API.Interfaces;
using System.Diagnostics;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling portfolio operations.
/// </summary>
public class PortfolioService(IDatabaseService dbService) : IPortfolioService
{

    /// <inheritdoc/>
    private async Task RefreshAllPortfolioStocksAsync(Portfolio portfolio)
    {
        var portfolioStocks = await dbService.GetPortfolioStocksAsync(portfolio.Id);

        foreach (var stock in portfolioStocks)
        {
            var latestIntraday = await dbService.GetLatestIntradayMarketDataAsync(stock.Symbol);
            var stockPrice = latestIntraday?.Current;

            if (stockPrice == null)
            {
                var latestHistorical = await dbService.GetLatestHistoricalMarketDataAsync(stock.Symbol);
                stockPrice = latestHistorical?.Close;
            }

            if (stockPrice != null)
            {
                stock.CurrentTotalValue = stock.Quantity * stockPrice.Value;
                stock.PercentageChange = ((stock.CurrentTotalValue - stock.TotalBaseValue) / stock.TotalBaseValue) * 100;
                stock.LastUpdated = DateTime.UtcNow.ToLocalTime();

                await dbService.UpdatePortfolioStockAsync(stock);
            }
            else
            {
                Debug.WriteLine($"Stock price not found for stock ID: {stock.Symbol}");
            }
        }
    }

    public async Task RefreshAllPortfoliosAsync()
    {
        var portfolios = await dbService.GetAllPortfoliosAsync();

        foreach (var portfolio in portfolios)
        {
            await RefreshAllPortfolioStocksAsync(portfolio);
        }
    }
}