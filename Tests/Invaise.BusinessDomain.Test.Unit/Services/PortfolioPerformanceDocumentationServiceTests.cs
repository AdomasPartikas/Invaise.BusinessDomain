using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Services;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using System.IO;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class PortfolioPerformanceDocumentationServiceTests : TestBase
{
    private readonly User _testUser;
    private readonly Portfolio _testPortfolio;
    private readonly List<PortfolioPerformance> _testPerformanceData;
    private readonly List<PortfolioStock> _testPortfolioStocks;
    private readonly PortfolioPerformanceReportModel _testReportModel;

    public PortfolioPerformanceDocumentationServiceTests()
    {
        // Set QuestPDF license for tests
        QuestPDF.Settings.License = LicenseType.Community;
        
        // Setup common test data
        _testUser = new User
        {
            Id = "user-123",
            DisplayName = "Test User",
            Email = "test@example.com",
            PersonalInfo = new UserPersonalInfo
            {
                Id = "personal-info-123",
                UserId = "user-123",
                PhoneNumber = "123-456-7890",
                Address = "123 Test St",
                City = "Test City",
                Country = "Test Country",
                LegalFirstName = "Test",
                LegalLastName = "User",
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        _testPortfolio = new Portfolio
        {
            Id = "portfolio-123",
            Name = "Test Portfolio",
            StrategyDescription = PortfolioStrategy.Balanced,
            CreatedAt = DateTime.Now.AddDays(-30),
            LastUpdated = DateTime.Now,
            User = _testUser
        };

        // Create performance history data
        _testPerformanceData = [];
        var baseDate = DateTime.Now.Date.AddDays(-10);
        
        for (int i = 0; i <= 10; i++)
        {
            var value = 10000m + (i * 100);
            var dailyChange = i == 0 ? 0m : 1.0m;
            var monthlyChange = i == 0 ? (decimal?)null : 5.0m;
            var yearlyChange = i == 0 ? (decimal?)null : 15.0m;
            
            _testPerformanceData.Add(new PortfolioPerformance
            {
                Date = baseDate.AddDays(i),
                TotalValue = value,
                TotalStocks = 5,
                DailyChangePercent = dailyChange,
                MonthlyChangePercent = monthlyChange,
                YearlyChangePercent = yearlyChange,
                Portfolio = _testPortfolio,
                PortfolioId = _testPortfolio.Id
            });
        }

        // Create portfolio stocks
        _testPortfolioStocks =
        [
            new() {
                Symbol = "AAPL",
                Quantity = 10,
                TotalBaseValue = 1500,
                CurrentTotalValue = 1600,
                PercentageChange = 6.67m,
                LastUpdated = DateTime.Now,
                PortfolioId = _testPortfolio.Id,
                Portfolio = _testPortfolio
            },
            new() {
                Symbol = "MSFT",
                Quantity = 5,
                TotalBaseValue = 1200,
                CurrentTotalValue = 1300,
                PercentageChange = 8.33m,
                LastUpdated = DateTime.Now,
                PortfolioId = _testPortfolio.Id,
                Portfolio = _testPortfolio
            },
            new() {
                Symbol = "GOOG",
                Quantity = 2,
                TotalBaseValue = 2000,
                CurrentTotalValue = 1900,
                PercentageChange = -5.0m,
                LastUpdated = DateTime.Now,
                PortfolioId = _testPortfolio.Id,
                Portfolio = _testPortfolio
            }
        ];
        
        _testPortfolio.PortfolioStocks = _testPortfolioStocks;

        _testReportModel = new PortfolioPerformanceReportModel
        {
            User = _testUser,
            Portfolio = _testPortfolio,
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now,
            PerformanceData = _testPerformanceData,
            PortfolioStocks = _testPortfolioStocks
        };
    }

    [Fact]
    public void Constructor_WithValidReportModel_CreatesInstance()
    {
        // Act
        var document = new PortfolioPerformanceDocumentationService(_testReportModel);

        // Assert
        Assert.NotNull(document);
    }

    [Fact]
    public void GeneratePdf_ProducesValidPdfDocument()
    {
        // Arrange
        var document = new PortfolioPerformanceDocumentationService(_testReportModel);
        
        // Act
        byte[] pdfBytes = document.GeneratePdf();
        
        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        
        Assert.Equal(0x25, pdfBytes[0]); // %
        Assert.Equal(0x50, pdfBytes[1]); // P
        Assert.Equal(0x44, pdfBytes[2]); // D
        Assert.Equal(0x46, pdfBytes[3]); // F
    }

    [Fact]
    public void Compose_WithEmptyPortfolioStocks_HandlesGracefully()
    {
        // Arrange
        var emptyStocksModel = new PortfolioPerformanceReportModel
        {
            User = _testUser,
            Portfolio = _testPortfolio,
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now,
            PerformanceData = _testPerformanceData,
            PortfolioStocks = [] // Empty stocks
        };
        
        var document = new PortfolioPerformanceDocumentationService(emptyStocksModel);
        
        // Act & Assert - Should not throw an exception
        var pdfBytes = document.GeneratePdf();
        Assert.NotNull(pdfBytes);
    }

    [Fact]
    public void Compose_WithEmptyPerformanceData_HandlesGracefully()
    {
        // Arrange
        var emptyPerformanceModel = new PortfolioPerformanceReportModel
        {
            User = _testUser,
            Portfolio = _testPortfolio,
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now,
            PerformanceData = [], // Empty performance data
            PortfolioStocks = _testPortfolioStocks
        };
        
        var document = new PortfolioPerformanceDocumentationService(emptyPerformanceModel);
        
        // Act & Assert - Should not throw an exception
        var pdfBytes = document.GeneratePdf();
        Assert.NotNull(pdfBytes);
    }

    [Fact]
    public void Compose_WithNullPerformanceValues_HandlesGracefully()
    {
        // Arrange
        var performanceWithNulls = new List<PortfolioPerformance>
        {
            new() {
                Date = DateTime.Now,
                TotalValue = 10000,
                TotalStocks = 5,
                DailyChangePercent = 0.0m,
                MonthlyChangePercent = null,
                YearlyChangePercent = null,
                Portfolio = _testPortfolio,
                PortfolioId = _testPortfolio.Id
            }
        };
        
        var modelWithNulls = new PortfolioPerformanceReportModel
        {
            User = _testUser,
            Portfolio = _testPortfolio,
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now,
            PerformanceData = performanceWithNulls,
            PortfolioStocks = _testPortfolioStocks
        };
        
        var document = new PortfolioPerformanceDocumentationService(modelWithNulls);
        
        // Act & Assert - Should not throw an exception
        var pdfBytes = document.GeneratePdf();
        Assert.NotNull(pdfBytes);
    }

    [Fact]
    public void Compose_WithUserWithoutPersonalInfo_HandlesGracefully()
    {
        // Arrange
        var userWithoutPersonalInfo = new User
        {
            Id = "user-no-info",
            DisplayName = "Test User No Info",
            Email = "test@example.com",
            PersonalInfo = null
        };
        
        var modelWithoutPersonalInfo = new PortfolioPerformanceReportModel
        {
            User = userWithoutPersonalInfo,
            Portfolio = _testPortfolio,
            StartDate = DateTime.Now.AddDays(-30),
            EndDate = DateTime.Now,
            PerformanceData = _testPerformanceData,
            PortfolioStocks = _testPortfolioStocks
        };
        
        var document = new PortfolioPerformanceDocumentationService(modelWithoutPersonalInfo);
        
        // Act & Assert - Should not throw an exception
        var pdfBytes = document.GeneratePdf();
        Assert.NotNull(pdfBytes);
    }
} 