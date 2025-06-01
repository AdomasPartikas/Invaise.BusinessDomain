using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class PortfolioPerformanceDocumentationServiceTests : TestBase
{
    [Fact]
    public void Constructor_WithValidReportModel_CreatesInstance()
    {
        // Arrange
        var user = new User
        {
            DisplayName = "Test User",
            Email = "test@example.com"
        };

        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            StrategyDescription = PortfolioStrategy.Balanced,
            CreatedAt = DateTime.Now.AddDays(-30),
            LastUpdated = DateTime.Now,
            User = user
        };

        var performanceData = new List<PortfolioPerformance>
        {
            new PortfolioPerformance
            {
                Date = DateTime.Now,
                TotalValue = 10000,
                TotalStocks = 5,
                Portfolio = portfolio,
                PortfolioId = portfolio.Id
            }
        };

        // Create a temporary list to set circular references properly
        var tempPortfolioStocks = new List<PortfolioStock>();
        var portfolioStock = new PortfolioStock
        {
            Symbol = "AAPL",
            Quantity = 10,
            TotalBaseValue = 1500,
            CurrentTotalValue = 1600,
            PercentageChange = 6.67m,
            LastUpdated = DateTime.Now,
            PortfolioId = portfolio.Id,
            Portfolio = portfolio
        };
        tempPortfolioStocks.Add(portfolioStock);
        
        // Set the portfolio stocks on the portfolio
        portfolio.PortfolioStocks = tempPortfolioStocks;

        var reportModel = new PortfolioPerformanceReportModel
        {
            User = user,
            Portfolio = portfolio,
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now,
            PerformanceData = performanceData,
            PortfolioStocks = tempPortfolioStocks
        };

        // Act
        var document = new PortfolioPerformanceDocumentationService(reportModel);

        // Assert
        Assert.NotNull(document);
        // We'll just test that the document can be created
    }
} 