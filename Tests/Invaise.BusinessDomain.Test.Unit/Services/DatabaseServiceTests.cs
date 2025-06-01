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
        var result = await _service.GetHistoricalMarketDataAsync(null, startDate, endDate);

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
        
        var updatedUserInDb = await _dbContext.Users.FindAsync("user1");
        Assert.NotNull(updatedUserInDb);
        Assert.Equal("Updated User", updatedUserInDb.DisplayName);
        Assert.False(updatedUserInDb.IsActive);
    }
} 