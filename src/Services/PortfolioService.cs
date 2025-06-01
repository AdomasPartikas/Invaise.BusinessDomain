using Invaise.BusinessDomain.API.Interfaces;
using System.Diagnostics;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Context;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling portfolio operations.
/// </summary>
public class PortfolioService(IDatabaseService dbService, InvaiseDbContext dbContext) : IPortfolioService
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

    /// <summary>
    /// Refreshes all portfolios by updating their associated stocks with the latest market data.
    /// </summary>
    public async Task RefreshAllPortfoliosAsync()
    {
        var portfolios = await dbService.GetAllPortfoliosAsync();

        foreach (var portfolio in portfolios)
        {
            await RefreshAllPortfolioStocksAsync(portfolio);
        }
    }
    
    /// <inheritdoc/>
    public async Task SaveEodPortfolioPerformanceAsync()
    {
        try
        {
            // Get all portfolios with their stocks
            var portfolios = await dbContext.Portfolios
                .Include(p => p.PortfolioStocks)
                .ToListAsync();
                
            foreach (var portfolio in portfolios)
            {
                // Calculate total value
                decimal totalValue = portfolio.PortfolioStocks.Sum(ps => ps.CurrentTotalValue);
                
                // Get previous day's performance to calculate daily change
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var previousPerformance = await dbContext.PortfolioPerformances
                    .Where(pp => pp.PortfolioId == portfolio.Id && pp.Date == yesterday)
                    .FirstOrDefaultAsync();
                
                // Calculate change percentages
                decimal dailyChangePercent = 0;
                if (previousPerformance != null && previousPerformance.TotalValue > 0)
                {
                    dailyChangePercent = ((totalValue - previousPerformance.TotalValue) / previousPerformance.TotalValue) * 100;
                }
                
                // Get week, month, and year ago performances
                var weekAgo = DateTime.UtcNow.Date.AddDays(-7);
                var monthAgo = DateTime.UtcNow.Date.AddMonths(-1);
                var yearAgo = DateTime.UtcNow.Date.AddYears(-1);
                
                var weekAgoPerformance = await dbContext.PortfolioPerformances
                    .Where(pp => pp.PortfolioId == portfolio.Id && pp.Date <= weekAgo)
                    .OrderByDescending(pp => pp.Date)
                    .FirstOrDefaultAsync();
                    
                var monthAgoPerformance = await dbContext.PortfolioPerformances
                    .Where(pp => pp.PortfolioId == portfolio.Id && pp.Date <= monthAgo)
                    .OrderByDescending(pp => pp.Date)
                    .FirstOrDefaultAsync();
                    
                var yearAgoPerformance = await dbContext.PortfolioPerformances
                    .Where(pp => pp.PortfolioId == portfolio.Id && pp.Date <= yearAgo)
                    .OrderByDescending(pp => pp.Date)
                    .FirstOrDefaultAsync();
                
                // Calculate other change percentages if data is available
                decimal? weeklyChangePercent = null;
                decimal? monthlyChangePercent = null;
                decimal? yearlyChangePercent = null;
                
                if (weekAgoPerformance != null && weekAgoPerformance.TotalValue > 0)
                {
                    weeklyChangePercent = ((totalValue - weekAgoPerformance.TotalValue) / weekAgoPerformance.TotalValue) * 100;
                }
                
                if (monthAgoPerformance != null && monthAgoPerformance.TotalValue > 0)
                {
                    monthlyChangePercent = ((totalValue - monthAgoPerformance.TotalValue) / monthAgoPerformance.TotalValue) * 100;
                }
                
                if (yearAgoPerformance != null && yearAgoPerformance.TotalValue > 0)
                {
                    yearlyChangePercent = ((totalValue - yearAgoPerformance.TotalValue) / yearAgoPerformance.TotalValue) * 100;
                }
                
                // Check if we already have a record for today
                var existingRecord = await dbContext.PortfolioPerformances
                    .FirstOrDefaultAsync(pp => pp.PortfolioId == portfolio.Id && pp.Date == DateTime.UtcNow.Date);
                
                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.TotalValue = totalValue;
                    existingRecord.DailyChangePercent = dailyChangePercent;
                    existingRecord.WeeklyChangePercent = weeklyChangePercent;
                    existingRecord.MonthlyChangePercent = monthlyChangePercent;
                    existingRecord.YearlyChangePercent = yearlyChangePercent;
                    existingRecord.TotalStocks = portfolio.PortfolioStocks.Count;
                    
                    dbContext.PortfolioPerformances.Update(existingRecord);
                }
                else
                {
                    // Create new performance record
                    var performanceRecord = new PortfolioPerformance
                    {
                        PortfolioId = portfolio.Id,
                        Date = DateTime.UtcNow.Date,
                        TotalValue = totalValue,
                        DailyChangePercent = dailyChangePercent,
                        WeeklyChangePercent = weeklyChangePercent,
                        MonthlyChangePercent = monthlyChangePercent,
                        YearlyChangePercent = yearlyChangePercent,
                        TotalStocks = portfolio.PortfolioStocks.Count,
                        CreatedAt = DateTime.UtcNow.ToLocalTime()
                    };
                    
                    dbContext.PortfolioPerformances.Add(performanceRecord);
                }
            }
            
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving EOD portfolio performance: {ex.Message}");
            throw;
        }
    }
    
    /// <inheritdoc/>
    public async Task<byte[]> GeneratePortfolioPerformancePdfAsync(string userId, string portfolioId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Set QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
            
            // Get user with personal info
            var user = await dbService.GetUserByIdAsync(userId);
            if (user == null)
                throw new ArgumentException($"User not found: {userId}");
                
            // Get portfolio with stocks
            var portfolio = await dbService.GetPortfolioByIdWithPortfolioStocksAsync(portfolioId);
            if (portfolio == null)
                throw new ArgumentException($"Portfolio not found: {portfolioId}");
                
            // Get portfolio stocks
            var portfolioStocks = await dbService.GetPortfolioStocksAsync(portfolioId);
                
            // Get performance data for the date range
            var performanceData = await dbContext.PortfolioPerformances
                .Where(pp => pp.PortfolioId == portfolioId && 
                            pp.Date >= startDate.Date && 
                            pp.Date <= endDate.Date)
                .OrderBy(pp => pp.Date)
                .ToListAsync();
                
            // Create the report model
            var reportModel = new PortfolioPerformanceReportModel
            {
                User = user,
                Portfolio = portfolio,
                PortfolioStocks = portfolioStocks,
                PerformanceData = performanceData,
                StartDate = startDate,
                EndDate = endDate,
                GenerationDate = DateTime.UtcNow.ToLocalTime()
            };
            
            // Generate the PDF
            var document = new PortfolioPerformanceDocumentationService(reportModel);
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating portfolio performance PDF: {ex.Message}");
            throw;
        }
    }
}