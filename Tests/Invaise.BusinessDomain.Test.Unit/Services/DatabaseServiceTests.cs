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
    private readonly DbContextOptions<InvaiseDbContext> _options;

    public DatabaseServiceTests()
    {
        // Setup in-memory database
        _options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: $"DatabaseServiceTestDb_{Guid.NewGuid()}")
            .Options;

        // Create DbContext with in-memory database
        _dbContext = new InvaiseDbContext(_options);
        
        // Create test data
        SeedDatabase();
        
        // Create service with real DbContext
        _service = new DatabaseService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        // Add sample market data
        _dbContext.HistoricalMarketData.AddRange(new List<HistoricalMarketData>
        {
            new HistoricalMarketData 
            { 
                Id = 1, 
                Symbol = "AAPL", 
                Date = DateTime.UtcNow.AddDays(-3),
                Open = 150.0m,
                High = 155.0m,
                Low = 149.0m,
                Close = 153.0m,
                Volume = 1000000
            },
            new HistoricalMarketData 
            { 
                Id = 2, 
                Symbol = "AAPL", 
                Date = DateTime.UtcNow.AddDays(-2),
                Open = 153.0m,
                High = 158.0m,
                Low = 152.0m,
                Close = 157.0m,
                Volume = 1100000
            },
            new HistoricalMarketData 
            { 
                Id = 3, 
                Symbol = "MSFT", 
                Date = DateTime.UtcNow.AddDays(-3),
                Open = 250.0m,
                High = 255.0m,
                Low = 248.0m,
                Close = 252.0m,
                Volume = 800000
            }
        });
        
        // Add sample intraday data
        _dbContext.IntradayMarketData.AddRange(new List<IntradayMarketData>
        {
            new IntradayMarketData 
            { 
                Id = 1, 
                Symbol = "AAPL", 
                Timestamp = DateTime.UtcNow.AddHours(-3),
                Open = 153.0m,
                High = 154.0m,
                Low = 152.0m,
                Current = 153.5m
            },
            new IntradayMarketData 
            { 
                Id = 2, 
                Symbol = "AAPL", 
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Open = 153.5m,
                High = 155.0m,
                Low = 153.0m,
                Current = 154.0m
            },
            new IntradayMarketData 
            { 
                Id = 3, 
                Symbol = "MSFT", 
                Timestamp = DateTime.UtcNow.AddHours(-3),
                Open = 252.0m,
                High = 253.0m,
                Low = 251.5m,
                Current = 252.5m
            }
        });
        
        // Add sample users
        _dbContext.Users.AddRange(new List<User>
        {
            new User
            {
                Id = "user1",
                Email = "user1@example.com",
                DisplayName = "User One",
                IsActive = true,
                PasswordHash = "hash", // Required
                Role = "User", // Required
                EmailVerified = true, // Required
                Preferences = new UserPreferences
                {
                    Id = "pref1",
                    UserId = "user1"
                },
                PersonalInfo = new UserPersonalInfo
                {
                    Id = "info1",
                    UserId = "user1",
                    DateOfBirth = DateTime.Parse("1990-01-01"),
                    PhoneNumber = "1234567890"
                }
            },
            new User
            {
                Id = "user2",
                Email = "user2@example.com",
                DisplayName = "User Two",
                IsActive = false,
                PasswordHash = "hash", // Required
                Role = "User", // Required
                EmailVerified = true, // Required
                Preferences = new UserPreferences
                {
                    Id = "pref2",
                    UserId = "user2"
                },
                PersonalInfo = new UserPersonalInfo
                {
                    Id = "info2",
                    UserId = "user2",
                    DateOfBirth = DateTime.Parse("1985-05-15"),
                    PhoneNumber = "0987654321"
                }
            }
        });
        
        // Add sample portfolios
        _dbContext.Portfolios.AddRange(new List<Portfolio>
        {
            new Portfolio
            {
                Id = "portfolio1",
                UserId = "user1",
                Name = "Portfolio One",
                StrategyDescription = PortfolioStrategy.Balanced,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                LastUpdated = DateTime.UtcNow.AddDays(-2)
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
        Assert.Equal(1, resultList.Count);
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
        Assert.NotNull(result.Preferences);
        Assert.NotNull(result.PersonalInfo);
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
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExcludeInactive_DoesNotReturnInactiveUsers()
    {
        // Arrange
        string userId = "user2";

        // Act
        var result = await _service.GetUserByIdAsync(userId, includeInactive: false);

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
    public async Task GetAllUsersAsync_OnlyActive_ReturnsOnlyActiveUsers()
    {
        // Act
        var result = await _service.GetAllUsersAsync(includeInactive: false);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
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
    public async Task CreateUserAsync_AddsUserToContext_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = "user3",
            Email = "user3@example.com",
            DisplayName = "User Three",
            IsActive = true,
            PasswordHash = "hash", // Required
            Role = "User", // Required
            EmailVerified = true // Required
        };

        // Act
        var result = await _service.CreateUserAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user3@example.com", result.Email);
        
        // Verify user was added to database
        var addedUser = await _dbContext.Users.FindAsync("user3");
        Assert.NotNull(addedUser);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesAndSavesChanges_ReturnsUpdatedUser()
    {
        // Arrange
        var user = await _dbContext.Users.FindAsync("user1");
        Assert.NotNull(user);
        user.DisplayName = "Updated User One";

        // Act
        var result = await _service.UpdateUserAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated User One", result.DisplayName);
        
        // Verify user was updated in database
        var updatedUser = await _dbContext.Users.FindAsync("user1");
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated User One", updatedUser.DisplayName);
    }
} 