using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class PortfolioServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly PortfolioService _service;
    private readonly Mock<IDatabaseService> _dbServiceMock;

    public PortfolioServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: $"PortfolioServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new InvaiseDbContext(options);
        
        SeedDatabase();
        
        _dbServiceMock = new Mock<IDatabaseService>();
        
        _service = new PortfolioService(_dbServiceMock.Object, _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        _dbContext.Users.AddRange(new List<User>
        {
            new() {
                Id = "user1",
                Email = "user1@example.com",
                DisplayName = "User One",
                IsActive = true,
                PasswordHash = "hash",
                Role = "User",
                EmailVerified = true
            }
        });
        
        _dbContext.Portfolios.AddRange(new List<Portfolio>
        {
            new() {
                Id = "portfolio1",
                UserId = "user1",
                Name = "Portfolio One",
                StrategyDescription = PortfolioStrategy.Balanced,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastUpdated = DateTime.UtcNow.AddDays(-2)
            },
            new() {
                Id = "portfolio2",
                UserId = "user1",
                Name = "Portfolio Two",
                StrategyDescription = PortfolioStrategy.Aggressive,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            }
        });
        
        _dbContext.PortfolioStocks.AddRange(new List<PortfolioStock>
        {
            new() {
                ID = "ps1",
                PortfolioId = "portfolio1",
                Symbol = "AAPL",
                Quantity = 10,
                CurrentTotalValue = 1530.0m,
                TotalBaseValue = 1500.0m,
                PercentageChange = 2.0m,
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Portfolio = null!
            },
            new() {
                ID = "ps2",
                PortfolioId = "portfolio1",
                Symbol = "MSFT",
                Quantity = 5,
                CurrentTotalValue = 1300.0m,
                TotalBaseValue = 1250.0m,
                PercentageChange = 4.0m,
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Portfolio = null!
            },
            new() {
                ID = "ps3",
                PortfolioId = "portfolio2",
                Symbol = "GOOG",
                Quantity = 2,
                CurrentTotalValue = 3150.0m,
                TotalBaseValue = 3000.0m,
                PercentageChange = 5.0m,
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Portfolio = null!
            }
        });
        
        _dbContext.PortfolioPerformances.AddRange(new List<PortfolioPerformance>
        {
            new() {
                Id = "perf1",
                PortfolioId = "portfolio1",
                Date = DateTime.UtcNow.Date.AddDays(-7),
                TotalValue = 2700.0m,
                DailyChangePercent = 1.5m,
                WeeklyChangePercent = 3.0m,
                MonthlyChangePercent = 5.0m,
                YearlyChangePercent = 15.0m,
                TotalStocks = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new() {
                Id = "perf2",
                PortfolioId = "portfolio1",
                Date = DateTime.UtcNow.Date.AddDays(-1),
                TotalValue = 2830.0m,
                DailyChangePercent = 1.0m,
                WeeklyChangePercent = 4.0m,
                MonthlyChangePercent = 6.0m,
                YearlyChangePercent = 16.0m,
                TotalStocks = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        });
        
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task SaveEodPortfolioPerformanceAsync_CreateNewRecordForToday_WhenNoRecordExistsForToday()
    {
        // Arrange
        _dbServiceMock.Setup(service => service.GetPortfolioStocksAsync("portfolio1"))
            .ReturnsAsync(await _dbContext.PortfolioStocks.Where(ps => ps.PortfolioId == "portfolio1").ToListAsync());
        
        _dbServiceMock.Setup(service => service.GetPortfolioStocksAsync("portfolio2"))
            .ReturnsAsync(await _dbContext.PortfolioStocks.Where(ps => ps.PortfolioId == "portfolio2").ToListAsync());
        
        var initialCount = await _dbContext.PortfolioPerformances.CountAsync();

        // Act
        await _service.SaveEodPortfolioPerformanceAsync();

        // Assert
        var finalCount = await _dbContext.PortfolioPerformances.CountAsync();
        Assert.Equal(initialCount + 2, finalCount);
        
        var todayRecords = await _dbContext.PortfolioPerformances
            .Where(pp => pp.Date == DateTime.UtcNow.Date)
            .ToListAsync();
            
        Assert.Equal(2, todayRecords.Count);
        
        var portfolio1Record = todayRecords.FirstOrDefault(pp => pp.PortfolioId == "portfolio1");
        Assert.NotNull(portfolio1Record);
        Assert.Equal(2830.0m, portfolio1Record.TotalValue);
        
        var portfolio2Record = todayRecords.FirstOrDefault(pp => pp.PortfolioId == "portfolio2");
        Assert.NotNull(portfolio2Record);
        Assert.Equal(3150.0m, portfolio2Record.TotalValue);
    }
    
    [Fact]
    public async Task SaveEodPortfolioPerformanceAsync_UpdatesExistingRecord_WhenRecordExistsForToday()
    {
        // Arrange
        var todayRecord = new PortfolioPerformance
        {
            Id = "perf-today",
            PortfolioId = "portfolio1",
            Date = DateTime.UtcNow.Date,
            TotalValue = 2800.0m,
            DailyChangePercent = 0.5m,
            WeeklyChangePercent = 2.0m,
            MonthlyChangePercent = 4.0m,
            YearlyChangePercent = 14.0m,
            TotalStocks = 2,
            CreatedAt = DateTime.UtcNow.AddHours(-6)
        };
        
        _dbContext.PortfolioPerformances.Add(todayRecord);
        await _dbContext.SaveChangesAsync();
        
        _dbServiceMock.Setup(service => service.GetPortfolioStocksAsync("portfolio1"))
            .ReturnsAsync(await _dbContext.PortfolioStocks.Where(ps => ps.PortfolioId == "portfolio1").ToListAsync());
        
        _dbServiceMock.Setup(service => service.GetPortfolioStocksAsync("portfolio2"))
            .ReturnsAsync(await _dbContext.PortfolioStocks.Where(ps => ps.PortfolioId == "portfolio2").ToListAsync());
        
        var initialCount = await _dbContext.PortfolioPerformances.CountAsync();

        // Act
        await _service.SaveEodPortfolioPerformanceAsync();

        // Assert
        var finalCount = await _dbContext.PortfolioPerformances.CountAsync();
        Assert.Equal(initialCount + 1, finalCount);
        
        var updatedRecord = await _dbContext.PortfolioPerformances
            .FirstOrDefaultAsync(pp => pp.Id == "perf-today");
            
        Assert.NotNull(updatedRecord);
        Assert.Equal(2830.0m, updatedRecord.TotalValue);
    }
    
    [Fact]
    public async Task RefreshAllPortfoliosAsync_CallsRefreshForEachPortfolio()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new() { Id = "portfolio1" },
            new() { Id = "portfolio2" }
        };
        
        _dbServiceMock.Setup(service => service.GetAllPortfoliosAsync())
            .ReturnsAsync(portfolios);
            
        var portfolio1Stocks = new List<PortfolioStock>
        {
            new() { 
                ID = "ps1", 
                Symbol = "AAPL", 
                Quantity = 10,
                PortfolioId = "portfolio1",
                CurrentTotalValue = 1500.0m,
                TotalBaseValue = 1500.0m,
                PercentageChange = 0.0m,
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Portfolio = null!
            },
            new() { 
                ID = "ps2", 
                Symbol = "MSFT", 
                Quantity = 5,
                PortfolioId = "portfolio1",
                CurrentTotalValue = 1250.0m,
                TotalBaseValue = 1250.0m,
                PercentageChange = 0.0m,
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Portfolio = null!
            }
        };
        
        var portfolio2Stocks = new List<PortfolioStock>
        {
            new() { 
                ID = "ps3", 
                Symbol = "GOOG", 
                Quantity = 2,
                PortfolioId = "portfolio2",
                CurrentTotalValue = 3000.0m,
                TotalBaseValue = 3000.0m,
                PercentageChange = 0.0m,
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Portfolio = null!
            }
        };
            
        _dbServiceMock.Setup(service => service.GetPortfolioStocksAsync("portfolio1"))
            .ReturnsAsync(portfolio1Stocks);
            
        _dbServiceMock.Setup(service => service.GetPortfolioStocksAsync("portfolio2"))
            .ReturnsAsync(portfolio2Stocks);
            
        var appleData = new IntradayMarketData { 
            Symbol = "AAPL", 
            Open = 159.0m,
            High = 160.5m,
            Low = 158.5m,
            Current = 160.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        var msftData = new IntradayMarketData { 
            Symbol = "MSFT", 
            Open = 259.0m,
            High = 260.5m,
            Low = 258.5m,
            Current = 260.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        var googData = new IntradayMarketData { 
            Symbol = "GOOG", 
            Open = 1599.0m,
            High = 1600.5m,
            Low = 1598.5m,
            Current = 1600.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("AAPL"))
            .ReturnsAsync(appleData);
            
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("MSFT"))
            .ReturnsAsync(msftData);
            
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("GOOG"))
            .ReturnsAsync(googData);

        // Act
        await _service.RefreshAllPortfoliosAsync();

        // Assert
        _dbServiceMock.Verify(service => service.GetAllPortfoliosAsync(), Times.Once);
        _dbServiceMock.Verify(service => service.GetPortfolioStocksAsync("portfolio1"), Times.Once);
        _dbServiceMock.Verify(service => service.GetPortfolioStocksAsync("portfolio2"), Times.Once);
        _dbServiceMock.Verify(service => service.GetLatestIntradayMarketDataAsync("AAPL"), Times.Once);
        _dbServiceMock.Verify(service => service.GetLatestIntradayMarketDataAsync("MSFT"), Times.Once);
        _dbServiceMock.Verify(service => service.GetLatestIntradayMarketDataAsync("GOOG"), Times.Once);
        _dbServiceMock.Verify(service => service.UpdatePortfolioStockAsync(It.IsAny<PortfolioStock>()), Times.Exactly(3));
    }
} 