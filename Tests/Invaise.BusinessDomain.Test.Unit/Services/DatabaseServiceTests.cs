using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class DatabaseServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly DatabaseService _service;

    public DatabaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new InvaiseDbContext(options);
        
        SeedDatabase();
        
        _service = new DatabaseService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        _dbContext.HistoricalMarketData.AddRange(new List<HistoricalMarketData>
        {
            new() {
                Id = 1,
                Symbol = "AAPL",
                Date = DateTime.UtcNow.Date.AddDays(-2),
                Open = 150.0m,
                High = 155.0m,
                Low = 149.0m,
                Close = 153.0m,
                Volume = 1000000
            },
            new() {
                Id = 2,
                Symbol = "AAPL",
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Open = 153.0m,
                High = 157.0m,
                Low = 152.0m,
                Close = 156.0m,
                Volume = 1200000
            },
            new() {
                Id = 3,
                Symbol = "MSFT",
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Open = 250.0m,
                High = 255.0m,
                Low = 248.0m,
                Close = 253.0m,
                Volume = 800000
            }
        });
        
        _dbContext.IntradayMarketData.AddRange(new List<IntradayMarketData>
        {
            new() {
                Id = 1,
                Symbol = "AAPL",
                Current = 156.5m,
                Open = 156.0m,
                High = 158.0m,
                Low = 155.0m,
                Timestamp = DateTime.UtcNow.AddMinutes(-30)
            },
            new() {
                Id = 2,
                Symbol = "AAPL",
                Current = 157.0m,
                Open = 156.5m,
                High = 159.0m,
                Low = 156.0m,
                Timestamp = DateTime.UtcNow.AddMinutes(-15)
            },
            new() {
                Id = 3,
                Symbol = "MSFT",
                Current = 254.0m,
                Open = 253.0m,
                High = 256.0m,
                Low = 252.0m,
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            }
        });
        
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
            },
            new() {
                Id = "user2",
                Email = "user2@example.com",
                DisplayName = "User Two",
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
                UserId = "user2",
                Name = "Portfolio Two",
                StrategyDescription = PortfolioStrategy.Conservative,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            }
        });

        _dbContext.Transactions.AddRange(new List<Transaction>
        {
            new() {
                Id = "trans1",
                UserId = "user1",
                PortfolioId = "portfolio1",
                Symbol = "AAPL",
                Type = TransactionType.Buy,
                Quantity = 10,
                PricePerShare = 150.0m,
                TransactionValue = 1500.0m,
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                TriggeredBy = AvailableTransactionTriggers.System,
                Status = TransactionStatus.Succeeded
            },
            new() {
                Id = "trans2",
                UserId = "user2",
                PortfolioId = "portfolio2",
                Symbol = "MSFT",
                Type = TransactionType.Sell,
                Quantity = 5,
                PricePerShare = 250.0m,
                TransactionValue = 1250.0m,
                TransactionDate = DateTime.UtcNow.AddHours(-2),
                TriggeredBy = AvailableTransactionTriggers.System,
                Status = TransactionStatus.OnHold
            }
        });

        _dbContext.PortfolioStocks.AddRange(new List<PortfolioStock>
        {
            new() {
                ID = "stock1",
                PortfolioId = "portfolio1",
                Symbol = "AAPL",
                Quantity = 10,
                CurrentTotalValue = 1560.0m,
                TotalBaseValue = 1500.0m,
                PercentageChange = 4.0m,
                LastUpdated = DateTime.UtcNow.AddMinutes(-30),
                Portfolio = null!
            },
            new() {
                ID = "stock2",
                PortfolioId = "portfolio2",
                Symbol = "MSFT",
                Quantity = 5,
                CurrentTotalValue = 1270.0m,
                TotalBaseValue = 1250.0m,
                PercentageChange = 1.6m,
                LastUpdated = DateTime.UtcNow.AddMinutes(-15),
                Portfolio = null!
            }
        });

        _dbContext.Companies.AddRange(new List<Company>
        {
            new() {
                StockId = 1,
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Industry = "Consumer Electronics",
                Description = "Technology company",
                Country = "United States"
            },
            new() {
                StockId = 2,
                Symbol = "MSFT",
                Name = "Microsoft Corporation",
                Industry = "Software",
                Description = "Software company",
                Country = "United States"
            }
        });

        _dbContext.ServiceAccounts.AddRange(new List<ServiceAccount>
        {
            new() {
                Id = "service1",
                Name = "Test Service",
                Key = "test-key",
                Role = "Service",
                Created = DateTime.UtcNow.AddDays(-10)
            }
        });

        _dbContext.LogEvents.AddRange(new List<Log>
        {
            new() {
                Id = 1,
                Level = "Info",
                Message = "Test log 1",
                TimeStamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new() {
                Id = 2,
                Level = "Error",
                Message = "Test log 2",
                TimeStamp = DateTime.UtcNow.AddMinutes(-3)
            }
        });
        
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllUniqueMarketDataSymbolsAsync_ReturnsUniqueSymbols()
    {
        // Act
        var result = await _service.GetAllUniqueMarketDataSymbolsAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains("AAPL", resultList);
        Assert.Contains("MSFT", resultList);
    }

    [Fact]
    public async Task GetHistoricalMarketDataAsync_WithSymbol_ReturnsFilteredData()
    {
        // Arrange
        string symbol = "AAPL";

        // Act
        var result = await _service.GetHistoricalMarketDataAsync(symbol, null, null);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, data => Assert.Equal(symbol, data.Symbol));
    }

    [Fact]
    public async Task GetHistoricalMarketDataAsync_WithDateRange_ReturnsFilteredData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-3);
        var endDate = DateTime.UtcNow.AddDays(-2);

        // Act
        var result = await _service.GetHistoricalMarketDataAsync(string.Empty, startDate, endDate);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.All(resultList, data => Assert.True(data.Date >= startDate && data.Date <= endDate));
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataAsync_ReturnsLatestData()
    {
        // Arrange
        string symbol = "AAPL";

        // Act
        var result = await _service.GetLatestHistoricalMarketDataAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(2, result.Id); // The second entry is the latest one
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataAsync_WithCount_ReturnsSpecifiedNumberOfItems()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 2;

        // Act
        var result = await _service.GetLatestHistoricalMarketDataAsync(symbol, count);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(count, resultList.Count);
        Assert.All(resultList, data => Assert.Equal(symbol, data.Symbol));
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataAsync_WithZeroCount_ReturnsOneItem()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 0;

        // Act
        var result = await _service.GetLatestHistoricalMarketDataAsync(symbol, count);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataAsync_WithNegativeCount_ReturnsOneItem()
    {
        // Arrange
        string symbol = "AAPL";
        int count = -5;

        // Act
        var result = await _service.GetLatestHistoricalMarketDataAsync(symbol, count);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataAsync_NonExistentSymbol_ReturnsNull()
    {
        // Arrange
        string symbol = "NONEXISTENT";

        // Act
        var result = await _service.GetLatestHistoricalMarketDataAsync(symbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataAsync_WithCount_NonExistentSymbol_ReturnsNull()
    {
        // Arrange
        string symbol = "NONEXISTENT";
        int count = 5;

        // Act
        var result = await _service.GetLatestHistoricalMarketDataAsync(symbol, count);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIntradayMarketDataAsync_WithSymbol_ReturnsFilteredData()
    {
        // Arrange
        string symbol = "AAPL";

        // Act
        var result = await _service.GetIntradayMarketDataAsync(symbol, null, null);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, data => Assert.Equal(symbol, data.Symbol));
    }

    [Fact]
    public async Task GetIntradayMarketDataAsync_WithDateRange_ReturnsFilteredData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMinutes(-20);
        var endDate = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var result = await _service.GetIntradayMarketDataAsync(string.Empty, startDate, endDate);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count); // AAPL second entry and MSFT entry
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataAsync_ReturnsLatestData()
    {
        // Arrange
        string symbol = "AAPL";

        // Act
        var result = await _service.GetLatestIntradayMarketDataAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(2, result.Id); // The second entry is the latest one
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataAsync_WithCount_ReturnsSpecifiedNumberOfItems()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 2;

        // Act
        var result = await _service.GetLatestIntradayMarketDataAsync(symbol, count);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(count, resultList.Count);
        Assert.All(resultList, data => Assert.Equal(symbol, data.Symbol));
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataAsync_WithZeroCount_ReturnsOneItem()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 0;

        // Act
        var result = await _service.GetLatestIntradayMarketDataAsync(symbol, count);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataAsync_NonExistentSymbol_ReturnsNull()
    {
        // Arrange
        string symbol = "NONEXISTENT";

        // Act
        var result = await _service.GetLatestIntradayMarketDataAsync(symbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataAsync_WithCount_NonExistentSymbol_ReturnsNull()
    {
        // Arrange
        string symbol = "NONEXISTENT";
        int count = 5;

        // Act
        var result = await _service.GetLatestIntradayMarketDataAsync(symbol, count);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingId_ReturnsUser()
    {
        // Arrange
        string userId = "user1";

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("user1@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_IncludeInactive_ReturnsInactiveUsers()
    {
        // Arrange
        string userId = "user2";

        // Act
        var result = await _service.GetUserByIdAsync(userId, includeInactive: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExcludeInactive_DoesNotReturnInactiveUsers()
    {
        // Arrange
        string userId = "user2";

        // Act
        var result = await _service.GetUserByIdAsync(userId, includeInactive: false);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        string userId = "nonexistent";

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Arrange
        string email = "user1@example.com";

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal("user1", result.Id);
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        // Arrange
        string email = "nonexistent@example.com";

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_OnlyActive_ReturnsOnlyActiveUsers()
    {
        // Act
        var result = await _service.GetAllUsersAsync(includeInactive: false);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, user => Assert.True(user.IsActive));
    }

    [Fact]
    public async Task GetAllUsersAsync_IncludeInactive_ReturnsAllUsers()
    {
        // Act
        var result = await _service.GetAllUsersAsync(includeInactive: true);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
    }

    [Fact]
    public async Task GetUserPortfoliosAsync_ReturnsUserPortfolios()
    {
        // Arrange
        string userId = "user1";

        // Act
        var result = await _service.GetUserPortfoliosAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.All(resultList, portfolio => Assert.Equal(userId, portfolio.UserId));
    }

    [Fact]
    public async Task GetUserPortfoliosAsync_NonExistentUser_ReturnsEmpty()
    {
        // Arrange
        string userId = "nonexistent";

        // Act
        var result = await _service.GetUserPortfoliosAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Empty(resultList);
    }

    [Fact]
    public async Task CreateUserAsync_AddsUserToDatabase()
    {
        // Arrange
        var newUser = new User
        {
            Id = "new-user",
            Email = "newuser@example.com",
            DisplayName = "New User",
            IsActive = true,
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true
        };

        // Act
        var result = await _service.CreateUserAsync(newUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newUser.Id, result.Id);
        Assert.Equal(newUser.Email, result.Email);
        
        var userInDb = await _dbContext.Users.FindAsync("new-user");
        Assert.NotNull(userInDb);
        Assert.Equal("newuser@example.com", userInDb.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesExistingUser()
    {
        // Arrange
        var userToUpdate = await _dbContext.Users.FindAsync("user1");
        Assert.NotNull(userToUpdate);
        userToUpdate.DisplayName = "Updated User";
        userToUpdate.IsActive = false;

        // Act
        var result = await _service.UpdateUserAsync(userToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated User", result.DisplayName);
        Assert.False(result.IsActive);
        Assert.True(result.UpdatedAt.HasValue);
        
        var updatedUserInDb = await _dbContext.Users.FindAsync("user1");
        Assert.NotNull(updatedUserInDb);
        Assert.Equal("Updated User", updatedUserInDb.DisplayName);
        Assert.False(updatedUserInDb.IsActive);
    }

    // Portfolio Tests
    [Fact]
    public async Task GetAllPortfoliosAsync_ReturnsAllPortfolios()
    {
        // Act
        var result = await _service.GetAllPortfoliosAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
    }

    [Fact]
    public async Task GetPortfolioByIdAsync_ExistingId_ReturnsPortfolio()
    {
        // Arrange
        string portfolioId = "portfolio1";

        // Act
        var result = await _service.GetPortfolioByIdAsync(portfolioId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(portfolioId, result.Id);
        Assert.Equal("Portfolio One", result.Name);
    }

    [Fact]
    public async Task GetPortfolioByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        string portfolioId = "nonexistent";

        // Act
        var result = await _service.GetPortfolioByIdAsync(portfolioId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPortfolioByIdWithPortfolioStocksAsync_ExistingId_ReturnsPortfolioWithStocks()
    {
        // Arrange
        string portfolioId = "portfolio1";

        // Act
        var result = await _service.GetPortfolioByIdWithPortfolioStocksAsync(portfolioId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(portfolioId, result.Id);
        Assert.NotNull(result.PortfolioStocks);
        Assert.Single(result.PortfolioStocks);
    }

    [Fact]
    public async Task CreatePortfolioAsync_AddsPortfolioToDatabase()
    {
        // Arrange
        var newPortfolio = new Portfolio
        {
            Id = "new-portfolio",
            UserId = "user1",
            Name = "New Portfolio",
            StrategyDescription = PortfolioStrategy.Aggressive,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Act
        var result = await _service.CreatePortfolioAsync(newPortfolio);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newPortfolio.Id, result.Id);
        Assert.Equal(newPortfolio.Name, result.Name);
        
        var portfolioInDb = await _dbContext.Portfolios.FindAsync("new-portfolio");
        Assert.NotNull(portfolioInDb);
    }

    [Fact]
    public async Task UpdatePortfolioAsync_UpdatesExistingPortfolio()
    {
        // Arrange
        var portfolioToUpdate = await _dbContext.Portfolios.FindAsync("portfolio1");
        Assert.NotNull(portfolioToUpdate);
        portfolioToUpdate.Name = "Updated Portfolio";

        // Act
        var result = await _service.UpdatePortfolioAsync(portfolioToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Portfolio", result.Name);
        Assert.True(result.LastUpdated > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task DeletePortfolioAsync_ExistingPortfolio_ReturnsTrue()
    {
        // Arrange
        string portfolioId = "portfolio1";

        // Act
        var result = await _service.DeletePortfolioAsync(portfolioId);

        // Assert
        Assert.True(result);
        var deletedPortfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
        Assert.NotNull(deletedPortfolio); // Portfolio still exists but is marked inactive
        Assert.False(deletedPortfolio.IsActive); // Should be marked as inactive (soft delete)
    }

    [Fact]
    public async Task DeletePortfolioAsync_NonExistentPortfolio_ReturnsFalse()
    {
        // Arrange
        string portfolioId = "nonexistent";

        // Act
        var result = await _service.DeletePortfolioAsync(portfolioId);

        // Assert
        Assert.False(result);
    }

    // Transaction Tests
    [Fact]
    public async Task GetUserTransactionsAsync_ReturnsUserTransactions()
    {
        // Arrange
        string userId = "user1";

        // Act
        var result = await _service.GetUserTransactionsAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.All(resultList, transaction => Assert.Equal(userId, transaction.UserId));
    }

    [Fact]
    public async Task GetPortfolioTransactionsAsync_ReturnsPortfolioTransactions()
    {
        // Arrange
        string portfolioId = "portfolio1";

        // Act
        var result = await _service.GetPortfolioTransactionsAsync(portfolioId);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.All(resultList, transaction => Assert.Equal(portfolioId, transaction.PortfolioId));
    }

    [Fact]
    public async Task CreateTransactionAsync_AddsTransactionToDatabase()
    {
        // Arrange
        var newTransaction = new Transaction
        {
            Id = "new-transaction",
            UserId = "user1",
            PortfolioId = "portfolio1",
            Symbol = "GOOGL",
            Type = TransactionType.Buy,
            Quantity = 5,
            PricePerShare = 2000.0m,
            TransactionValue = 10000.0m,
            TransactionDate = DateTime.UtcNow,
            TriggeredBy = AvailableTransactionTriggers.System,
            Status = TransactionStatus.Succeeded
        };

        // Act
        var result = await _service.CreateTransactionAsync(newTransaction);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newTransaction.Id, result.Id);
        Assert.Equal(newTransaction.Symbol, result.Symbol);
        
        var transactionInDb = await _dbContext.Transactions.FindAsync("new-transaction");
        Assert.NotNull(transactionInDb);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_ExistingId_ReturnsTransaction()
    {
        // Arrange
        string transactionId = "trans1";

        // Act
        var result = await _service.GetTransactionByIdAsync(transactionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transactionId, result.Id);
        Assert.Equal("AAPL", result.Symbol);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        string transactionId = "nonexistent";

        // Act
        var result = await _service.GetTransactionByIdAsync(transactionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelTransactionAsync_ExistingTransaction_CancelsTransaction()
    {
        // Arrange
        string transactionId = "trans2";

        // Act
        await _service.CancelTransactionAsync(transactionId);

        // Assert
        var transaction = await _dbContext.Transactions.FindAsync(transactionId);
        Assert.NotNull(transaction);
        Assert.Equal(TransactionStatus.Canceled, transaction.Status);
    }

    // PortfolioStock Tests
    [Fact]
    public async Task GetPortfolioStocksAsync_ReturnsPortfolioStocks()
    {
        // Arrange
        string portfolioId = "portfolio1";

        // Act
        var result = await _service.GetPortfolioStocksAsync(portfolioId);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.All(resultList, stock => Assert.Equal(portfolioId, stock.PortfolioId));
    }

    [Fact]
    public async Task GetPortfolioStockByIdAsync_ExistingId_ReturnsPortfolioStock()
    {
        // Arrange
        string stockId = "stock1";

        // Act
        var result = await _service.GetPortfolioStockByIdAsync(stockId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stockId, result.ID);
        Assert.Equal("AAPL", result.Symbol);
    }

    [Fact]
    public async Task GetPortfolioStockByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        string stockId = "nonexistent";

        // Act
        var result = await _service.GetPortfolioStockByIdAsync(stockId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddPortfolioStockAsync_AddsStockToDatabase()
    {
        // Arrange
        var newStock = new PortfolioStock
        {
            ID = "new-stock",
            PortfolioId = "portfolio1",
            Symbol = "GOOGL",
            Quantity = 5,
            CurrentTotalValue = 2000.0m,
            TotalBaseValue = 1500.0m,
            PercentageChange = 3.33m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = null!
        };

        // Act
        var result = await _service.AddPortfolioStockAsync(newStock);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newStock.ID, result.ID);
        Assert.Equal(newStock.Symbol, result.Symbol);
        
        var stockInDb = await _dbContext.PortfolioStocks.FindAsync("new-stock");
        Assert.NotNull(stockInDb);
    }

    [Fact]
    public async Task UpdatePortfolioStockAsync_UpdatesExistingStock()
    {
        // Arrange
        var stockToUpdate = await _dbContext.PortfolioStocks.FindAsync("stock1");
        Assert.NotNull(stockToUpdate);
        stockToUpdate.Quantity = 20;
        stockToUpdate.CurrentTotalValue = 2000.0m;
        stockToUpdate.TotalBaseValue = 1500.0m;
        stockToUpdate.PercentageChange = 3.33m;

        // Act
        var result = await _service.UpdatePortfolioStockAsync(stockToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.Quantity);
        Assert.Equal(2000.0m, result.CurrentTotalValue);
        Assert.Equal(1500.0m, result.TotalBaseValue);
        Assert.Equal(3.33m, result.PercentageChange);
    }

    [Fact]
    public async Task DeletePortfolioStockAsync_ExistingStock_ReturnsTrue()
    {
        // Arrange
        string stockId = "stock1";

        // Act
        var result = await _service.DeletePortfolioStockAsync(stockId);

        // Assert
        Assert.True(result);
        var deletedStock = await _dbContext.PortfolioStocks.FindAsync(stockId);
        Assert.Null(deletedStock);
    }

    [Fact]
    public async Task DeletePortfolioStockAsync_NonExistentStock_ReturnsFalse()
    {
        // Arrange
        string stockId = "nonexistent";

        // Act
        var result = await _service.DeletePortfolioStockAsync(stockId);

        // Assert
        Assert.False(result);
    }

    // Company Tests
    [Fact]
    public async Task GetAllCompaniesAsync_ReturnsAllCompanies()
    {
        // Act
        var result = await _service.GetAllCompaniesAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_ExistingId_ReturnsCompany()
    {
        // Arrange
        int companyId = 1;

        // Act
        var result = await _service.GetCompanyByIdAsync(companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(companyId, result.StockId);
        Assert.Equal("AAPL", result.Symbol);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        int companyId = 999;

        // Act
        var result = await _service.GetCompanyByIdAsync(companyId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompanyBySymbolAsync_ExistingSymbol_ReturnsCompany()
    {
        // Arrange
        string symbol = "AAPL";

        // Act
        var result = await _service.GetCompanyBySymbolAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal("Apple Inc.", result.Name);
    }

    [Fact]
    public async Task GetCompanyBySymbolAsync_NonExistentSymbol_ReturnsNull()
    {
        // Arrange
        string symbol = "NONEXISTENT";

        // Act
        var result = await _service.GetCompanyBySymbolAsync(symbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCompanyAsync_AddsCompanyToDatabase()
    {
        // Arrange
        var newCompany = new Company
        {
            Symbol = "GOOGL",
            Name = "Alphabet Inc.",
            Industry = "Internet Software",
            Description = "Technology company",
            Country = "United States"
        };

        // Act
        var result = await _service.CreateCompanyAsync(newCompany);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newCompany.Symbol, result.Symbol);
        Assert.Equal(newCompany.Name, result.Name);
        Assert.True(result.StockId > 0);
        
        var companyInDb = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Symbol == "GOOGL");
        Assert.NotNull(companyInDb);
    }

    [Fact]
    public async Task UpdateCompanyAsync_UpdatesExistingCompany()
    {
        // Arrange
        var companyToUpdate = await _dbContext.Companies.FindAsync(1);
        Assert.NotNull(companyToUpdate);
        companyToUpdate.Name = "Apple Corporation";

        // Act
        var result = await _service.UpdateCompanyAsync(companyToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Apple Corporation", result.Name);
        
        var updatedCompany = await _dbContext.Companies.FindAsync(1);
        Assert.NotNull(updatedCompany);
        Assert.Equal("Apple Corporation", updatedCompany.Name);
    }

    [Fact]
    public async Task DeleteCompanyAsync_ExistingCompany_ReturnsTrue()
    {
        // Arrange
        int companyId = 1;

        // Act
        var result = await _service.DeleteCompanyAsync(companyId);

        // Assert
        Assert.True(result);
        var deletedCompany = await _dbContext.Companies.FindAsync(companyId);
        Assert.Null(deletedCompany);
    }

    [Fact]
    public async Task DeleteCompanyAsync_NonExistentCompany_ReturnsFalse()
    {
        // Arrange
        int companyId = 999;

        // Act
        var result = await _service.DeleteCompanyAsync(companyId);

        // Assert
        Assert.False(result);
    }

    // Service Account Tests
    [Fact]
    public async Task CreateServiceAccountAsync_AddsServiceAccountToDatabase()
    {
        // Arrange
        var newServiceAccount = new ServiceAccount
        {
            Id = "new-service",
            Name = "New Test Service",
            Key = "new-key",
            Role = "Service",
            Created = DateTime.UtcNow
        };

        // Act
        await _service.CreateServiceAccountAsync(newServiceAccount);

        // Assert
        var serviceAccountInDb = await _dbContext.ServiceAccounts.FindAsync("new-service");
        Assert.NotNull(serviceAccountInDb);
        Assert.Equal("New Test Service", serviceAccountInDb.Name);
    }

    [Fact]
    public async Task GetServiceAccountAsync_ExistingId_ReturnsServiceAccount()
    {
        // Arrange
        string serviceAccountId = "service1";

        // Act
        var result = await _service.GetServiceAccountAsync(serviceAccountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serviceAccountId, result.Id);
        Assert.Equal("Test Service", result.Name);
    }

    [Fact]
    public async Task GetServiceAccountAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        string serviceAccountId = "nonexistent";

        // Act
        var result = await _service.GetServiceAccountAsync(serviceAccountId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateServiceAccountAsync_UpdatesExistingServiceAccount()
    {
        // Arrange
        string serviceAccountId = "service1";
        var updatedAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            Name = "Updated Test Service",
            Key = "updated-key",
            Role = "Service",
            Created = DateTime.UtcNow.AddDays(-5)
        };

        // Act
        var result = await _service.UpdateServiceAccountAsync(serviceAccountId, updatedAccount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Test Service", result.Name);
        Assert.Equal("updated-key", result.Key);
        
        var updatedInDb = await _dbContext.ServiceAccounts.FindAsync(serviceAccountId);
        Assert.NotNull(updatedInDb);
        Assert.Equal("Updated Test Service", updatedInDb.Name);
        Assert.Equal("updated-key", updatedInDb.Key);
    }

    // Log Tests
    [Fact]
    public async Task GetLatestLogsAsync_ReturnsSpecifiedNumberOfLogs()
    {
        // Arrange
        int count = 1;

        // Act
        var result = await _service.GetLatestLogsAsync(count);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(count, resultList.Count);
        Assert.Equal("Test log 2", resultList[0].Message); // Latest log
    }

    [Fact]
    public async Task GetLatestLogsAsync_WithLargerCount_ReturnsAllAvailableLogs()
    {
        // Arrange
        int count = 10;

        // Act
        var result = await _service.GetLatestLogsAsync(count);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count); // Only 2 logs available
    }

    // Additional edge case tests
    [Fact]
    public async Task GetHistoricalMarketDataAsync_EmptySymbol_ReturnsAllData()
    {
        // Act
        var result = await _service.GetHistoricalMarketDataAsync("", null, null);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count); // All historical data
    }

    [Fact]
    public async Task GetIntradayMarketDataAsync_EmptySymbol_ReturnsAllData()
    {
        // Act
        var result = await _service.GetIntradayMarketDataAsync("", null, null);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count); // All intraday data
    }

    [Fact]
    public void DatabaseService_ImplementsIDatabaseService()
    {
        // Assert
        Assert.IsAssignableFrom<IDatabaseService>(_service);
    }
} 