using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class TransactionServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly TransactionService _service;
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<IMarketDataService> _marketDataServiceMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new InvaiseDbContext(options);
        _dbServiceMock = new Mock<IDatabaseService>();
        _marketDataServiceMock = new Mock<IMarketDataService>();
        _loggerMock = new Mock<Serilog.ILogger>();

        var testUser = new User { Id = "test-user-id", DisplayName = "Test User", Email = "test@example.com" };
        var testPortfolio = new Portfolio { Id = "test-portfolio-id", UserId = "test-user-id", Name = "Test Portfolio" };

        _dbContext.Users.Add(testUser);
        _dbContext.Portfolios.Add(testPortfolio);
        _dbContext.SaveChanges();

        _service = new TransactionService(
            _dbContext,
            _dbServiceMock.Object,
            _marketDataServiceMock.Object,
            _loggerMock.Object
        );
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
            }
        });
        
        _dbContext.Transactions.AddRange(new List<Transaction>
        {
            new() {
                Id = "tx1",
                UserId = "user1",
                PortfolioId = "portfolio1",
                Symbol = "AAPL",
                Quantity = 5,
                PricePerShare = 150.0m,
                TransactionValue = 750.0m,
                TransactionDate = DateTime.UtcNow.AddDays(-5),
                Type = TransactionType.Buy,
                Status = TransactionStatus.Succeeded,
                TriggeredBy = AvailableTransactionTriggers.User
            },
            new() {
                Id = "tx2",
                UserId = "user1",
                PortfolioId = "portfolio1",
                Symbol = "MSFT",
                Quantity = 3,
                PricePerShare = 250.0m,
                TransactionValue = 750.0m,
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Type = TransactionType.Buy,
                Status = TransactionStatus.OnHold,
                TriggeredBy = AvailableTransactionTriggers.AI
            }
        });
        
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateTransactionFromRecommendationAsync_CreatesNewBuyTransaction_WhenTargetQuantityIsGreaterThanCurrent()
    {
        // Arrange
        SeedDatabase();
        
        string userId = "user1";
        string portfolioId = "portfolio1";
        string symbol = "GOOG";
        decimal currentQuantity = 0;
        decimal targetQuantity = 2;
        
        _marketDataServiceMock.Setup(service => service.IsMarketOpenAsync())
            .ReturnsAsync(false);
            
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync(symbol))
            .ReturnsAsync((IntradayMarketData?)null);
            
        _dbServiceMock.Setup(service => service.GetLatestHistoricalMarketDataAsync(symbol))
            .ReturnsAsync(new HistoricalMarketData { Close = 1500.0m });
            
        var initialTransactionCount = await _dbContext.Transactions.CountAsync();

        // Act
        var result = await _service.CreateTransactionFromRecommendationAsync(
            userId, portfolioId, symbol, currentQuantity, targetQuantity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Buy, result.Type);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(1500.0m, result.PricePerShare);
        Assert.Equal(3000.0m, result.TransactionValue);
        Assert.Equal(TransactionStatus.OnHold, result.Status);
        Assert.Equal(AvailableTransactionTriggers.AI, result.TriggeredBy);
        
        var finalTransactionCount = await _dbContext.Transactions.CountAsync();
        Assert.Equal(initialTransactionCount + 1, finalTransactionCount);
    }
    
    [Fact]
    public async Task CreateTransactionFromRecommendationAsync_CreatesNewSellTransaction_WhenTargetQuantityIsLessThanCurrent()
    {
        // Arrange
        SeedDatabase();
        
        string userId = "user1";
        string portfolioId = "portfolio1";
        string symbol = "AAPL";
        decimal currentQuantity = 10;
        decimal targetQuantity = 5;
        
        _marketDataServiceMock.Setup(service => service.IsMarketOpenAsync())
            .ReturnsAsync(false);
            
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync(symbol))
            .ReturnsAsync(new IntradayMarketData { Current = 153.0m });
            
        var initialTransactionCount = await _dbContext.Transactions.CountAsync();

        // Act
        var result = await _service.CreateTransactionFromRecommendationAsync(
            userId, portfolioId, symbol, currentQuantity, targetQuantity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Sell, result.Type);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(153.0m, result.PricePerShare);
        Assert.Equal(765.0m, result.TransactionValue);
        Assert.Equal(TransactionStatus.OnHold, result.Status);
        Assert.Equal(AvailableTransactionTriggers.AI, result.TriggeredBy);
        
        var finalTransactionCount = await _dbContext.Transactions.CountAsync();
        Assert.Equal(initialTransactionCount + 1, finalTransactionCount);
    }
    
    [Fact]
    public async Task CreateTransactionFromRecommendationAsync_ProcessesImmediately_WhenMarketIsOpen()
    {
        // Arrange
        SeedDatabase();
        
        string userId = "user1";
        string portfolioId = "portfolio1";
        string symbol = "AAPL";
        decimal currentQuantity = 10;
        decimal targetQuantity = 15;
        
        _marketDataServiceMock.Setup(service => service.IsMarketOpenAsync())
            .ReturnsAsync(true);
            
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync(symbol))
            .ReturnsAsync(new IntradayMarketData { Current = 153.0m });

        // Act
        var result = await _service.CreateTransactionFromRecommendationAsync(
            userId, portfolioId, symbol, currentQuantity, targetQuantity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Buy, result.Type);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(153.0m, result.PricePerShare);
        Assert.Equal(765.0m, result.TransactionValue);
        Assert.Equal(TransactionStatus.Succeeded, result.Status);
        
        var portfolioStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == portfolioId && ps.Symbol == symbol);
            
        Assert.NotNull(portfolioStock);
        Assert.Equal(15, portfolioStock.Quantity);
    }
    
    [Fact]
    public async Task ProcessPendingTransactionsAsync_ProcessesOnHoldTransactions_WhenMarketIsOpen()
    {
        // Arrange
        SeedDatabase();
        
        _marketDataServiceMock.Setup(service => service.IsMarketOpenAsync())
            .ReturnsAsync(true);
            
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("MSFT"))
            .ReturnsAsync(new IntradayMarketData { Current = 255.0m });

        // Act
        var result = await _service.ProcessPendingTransactionsAsync();

        // Assert
        Assert.Equal(1, result);
        
        var transaction = await _dbContext.Transactions.FindAsync("tx2");
        Assert.NotNull(transaction);
        Assert.Equal(TransactionStatus.Succeeded, transaction.Status);
        
        var portfolioStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "MSFT");
            
        Assert.NotNull(portfolioStock);
        Assert.Equal(3, portfolioStock.Quantity);
    }
    
    [Fact]
    public async Task ProcessPendingTransactionsAsync_DoesNotProcessTransactions_WhenMarketIsClosed()
    {
        // Arrange
        SeedDatabase();
        
        _marketDataServiceMock.Setup(service => service.IsMarketOpenAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _service.ProcessPendingTransactionsAsync();

        // Assert
        Assert.Equal(0, result);
        
        var transaction = await _dbContext.Transactions.FindAsync("tx2");
        Assert.NotNull(transaction);
        Assert.Equal(TransactionStatus.OnHold, transaction.Status);
    }
    
    [Fact]
    public async Task ApplyTransactionToPortfolioAsync_CreateNewPortfolioStock_WhenBuyingNewStock()
    {
        // Arrange
        SeedDatabase();
        
        var transaction = new Transaction
        {
            Id = "tx-new",
            UserId = "user1",
            PortfolioId = "portfolio1",
            Symbol = "NFLX",
            Quantity = 2,
            PricePerShare = 400.0m,
            TransactionValue = 800.0m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            Status = TransactionStatus.OnHold,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("NFLX"))
            .ReturnsAsync(new IntradayMarketData { Current = 400.0m });
            
        var initialStockCount = await _dbContext.PortfolioStocks.CountAsync();

        // Act
        var result = await _service.ApplyTransactionToPortfolioAsync(transaction);

        // Assert
        Assert.True(result);
        
        var finalStockCount = await _dbContext.PortfolioStocks.CountAsync();
        Assert.Equal(initialStockCount + 1, finalStockCount);
        
        var portfolioStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "NFLX");
            
        Assert.NotNull(portfolioStock);
        Assert.Equal(2, portfolioStock.Quantity);
        Assert.Equal(800.0m, portfolioStock.TotalBaseValue);
        Assert.Equal(800.0m, portfolioStock.CurrentTotalValue);
    }
    
    [Fact]
    public async Task ApplyTransactionToPortfolioAsync_UpdateExistingPortfolioStock_WhenBuyingMoreShares()
    {
        // Arrange
        SeedDatabase();
        
        var transaction = new Transaction
        {
            Id = "tx-buy-more",
            UserId = "user1",
            PortfolioId = "portfolio1",
            Symbol = "AAPL",
            Quantity = 5,
            PricePerShare = 155.0m,
            TransactionValue = 775.0m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            Status = TransactionStatus.OnHold,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("AAPL"))
            .ReturnsAsync(new IntradayMarketData { Current = 155.0m });
            
        var initialStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "AAPL");
            
        Assert.NotNull(initialStock);
        var initialQuantity = initialStock.Quantity;

        // Act
        var result = await _service.ApplyTransactionToPortfolioAsync(transaction);

        // Assert
        Assert.True(result);
        
        var updatedStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "AAPL");
            
        Assert.NotNull(updatedStock);
        Assert.Equal(initialQuantity + 5, updatedStock.Quantity);
        Assert.Equal(1500.0m + 775.0m, updatedStock.TotalBaseValue);
    }
    
    [Fact]
    public async Task ApplyTransactionToPortfolioAsync_UpdateExistingPortfolioStock_WhenSellingShares()
    {
        // Arrange
        SeedDatabase();
        
        var transaction = new Transaction
        {
            Id = "tx-sell",
            UserId = "user1",
            PortfolioId = "portfolio1",
            Symbol = "AAPL",
            Quantity = 5,
            PricePerShare = 155.0m,
            TransactionValue = 775.0m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Sell,
            Status = TransactionStatus.OnHold,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("AAPL"))
            .ReturnsAsync(new IntradayMarketData { Current = 155.0m });
            
        var initialStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "AAPL");
            
        Assert.NotNull(initialStock);
        var initialQuantity = initialStock.Quantity;

        // Act
        var result = await _service.ApplyTransactionToPortfolioAsync(transaction);

        // Assert
        Assert.True(result);
        
        var updatedStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "AAPL");
            
        Assert.NotNull(updatedStock);
        Assert.Equal(initialQuantity - 5, updatedStock.Quantity);
        Assert.Equal(750.0m, updatedStock.TotalBaseValue);
    }
    
    [Fact]
    public async Task ApplyTransactionToPortfolioAsync_RemovesPortfolioStock_WhenSellingAllShares()
    {
        // Arrange
        SeedDatabase();
        
        var transaction = new Transaction
        {
            Id = "tx-sell-all",
            UserId = "user1",
            PortfolioId = "portfolio1",
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 155.0m,
            TransactionValue = 1550.0m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Sell,
            Status = TransactionStatus.OnHold,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(service => service.GetLatestIntradayMarketDataAsync("AAPL"))
            .ReturnsAsync(new IntradayMarketData { Current = 155.0m });
            
        var initialStockCount = await _dbContext.PortfolioStocks.CountAsync();

        // Act
        var result = await _service.ApplyTransactionToPortfolioAsync(transaction);

        // Assert
        Assert.True(result);
        
        var finalStockCount = await _dbContext.PortfolioStocks.CountAsync();
        Assert.Equal(initialStockCount - 1, finalStockCount);
        
        var removedStock = await _dbContext.PortfolioStocks
            .FirstOrDefaultAsync(ps => ps.PortfolioId == "portfolio1" && ps.Symbol == "AAPL");
            
        Assert.Null(removedStock);
    }
} 