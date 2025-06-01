using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Constants;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Invaise.BusinessDomain.Test.Unit.Utilities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class PortfolioOptimizationServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly PortfolioOptimizationService _sut;
    private readonly Mock<IDatabaseService> _databaseServiceMock;
    private readonly Mock<IGaiaService> _gaiaServiceMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;
    private readonly Mock<IMapper> _mapperMock;

    public PortfolioOptimizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new InvaiseDbContext(options);
        _databaseServiceMock = new Mock<IDatabaseService>();
        _gaiaServiceMock = new Mock<IGaiaService>();
        _transactionServiceMock = new Mock<ITransactionService>();
        _loggerMock = new Mock<Serilog.ILogger>();
        _mapperMock = new Mock<IMapper>();

        _sut = new PortfolioOptimizationService(
            _databaseServiceMock.Object,
            _dbContext,
            _gaiaServiceMock.Object,
            _transactionServiceMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase(List<PortfolioOptimization>? optimizations = null, List<Portfolio>? portfolios = null)
    {
        if (optimizations != null)
        {
            _dbContext.PortfolioOptimizations.AddRange(optimizations);
        }
        
        if (portfolios != null)
        {
            _dbContext.Portfolios.AddRange(portfolios);
        }
        
        _dbContext.SaveChanges();
    }
    
    #region HasOngoingOptimizationAsync
    
    [Fact]
    public async Task HasOngoingOptimizationAsync_WithOngoingOptimization_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = "opt1", 
                UserId = userId, 
                PortfolioId = portfolioId, 
                Status = PortfolioOptimizationStatus.InProgress 
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.HasOngoingOptimizationAsync(userId, portfolioId);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task HasOngoingOptimizationAsync_WithCreatedOptimization_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = "opt1", 
                UserId = userId, 
                PortfolioId = portfolioId, 
                Status = PortfolioOptimizationStatus.Created 
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.HasOngoingOptimizationAsync(userId, portfolioId);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task HasOngoingOptimizationAsync_WithNoOngoingOptimization_ReturnsFalse()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = "opt1", 
                UserId = userId, 
                PortfolioId = portfolioId, 
                Status = PortfolioOptimizationStatus.Applied 
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.HasOngoingOptimizationAsync(userId, portfolioId);
        
        // Assert
        result.Should().BeFalse();
    }
    
    #endregion
    
    #region GetRemainingCoolOffTime
    
    [Fact]
    public async Task GetRemainingCoolOffTime_NoOptimizations_ReturnsZero()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        // Act
        var result = await _sut.GetRemainingCoolOffTime(userId, portfolioId);
        
        // Assert
        result.Should().Be(TimeSpan.Zero);
    }
    
    [Fact]
    public async Task GetRemainingCoolOffTime_NotAppliedStatus_ReturnsZero()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = "opt1", 
                UserId = userId, 
                PortfolioId = portfolioId, 
                Status = PortfolioOptimizationStatus.Created,
                Timestamp = DateTime.UtcNow
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.GetRemainingCoolOffTime(userId, portfolioId);
        
        // Assert
        result.Should().Be(TimeSpan.Zero);
    }
    
    [Fact]
    public async Task GetRemainingCoolOffTime_AppliedButExpired_ReturnsZero()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var appliedDate = DateTime.UtcNow.AddHours(-GaiaConstants.OptimizationCoolOffPeriod - 1);
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = "opt1", 
                UserId = userId, 
                PortfolioId = portfolioId, 
                Status = PortfolioOptimizationStatus.Applied,
                AppliedDate = appliedDate,
                Timestamp = appliedDate
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.GetRemainingCoolOffTime(userId, portfolioId);
        
        // Assert
        result.Should().Be(TimeSpan.Zero);
    }
    
    [Fact]
    public async Task GetRemainingCoolOffTime_AppliedWithinCooloff_ReturnsRemainingTime()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var appliedDate = DateTime.UtcNow.AddHours(-GaiaConstants.OptimizationCoolOffPeriod + 2);
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = "opt1", 
                UserId = userId, 
                PortfolioId = portfolioId, 
                Status = PortfolioOptimizationStatus.Applied,
                AppliedDate = appliedDate,
                Timestamp = appliedDate
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.GetRemainingCoolOffTime(userId, portfolioId);
        
        // Assert
        result.Should().BeGreaterThan(TimeSpan.Zero);
        result.Should().BeLessThan(TimeSpan.FromHours(2.1)); 
    }
    
    #endregion
    
    #region GetOptimizationStatusAsync
    
    [Fact]
    public async Task GetOptimizationStatusAsync_OptimizationExists_ReturnsStatus()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = optimizationId, 
                UserId = userId, 
                PortfolioId = "portfolio123", 
                Status = PortfolioOptimizationStatus.Applied 
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.GetOptimizationStatusAsync(userId, optimizationId);
        
        // Assert
        result.Should().Be(PortfolioOptimizationStatus.Applied);
    }
    
    [Fact]
    public async Task GetOptimizationStatusAsync_OptimizationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "nonexistent";
        
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetOptimizationStatusAsync(userId, optimizationId));
    }
    
    #endregion
    
    #region OptimizePortfolioAsync
    
    [Fact]
    public async Task OptimizePortfolioAsync_ExistingOptimization_ReturnsExistingOptimization()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var existingOptimization = new PortfolioOptimization
        {
            Id = "opt1",
            UserId = userId,
            PortfolioId = portfolioId,
            Status = PortfolioOptimizationStatus.Created,
            Explanation = "Existing optimization",
            Confidence = 0.85,
            Recommendations = new List<PortfolioOptimizationRecommendation>()
        };
        
        var optimizations = new List<PortfolioOptimization> { existingOptimization };
        SeedDatabase(optimizations);
        
        var expectedResult = new PortfolioOptimizationResult
        {
            OptimizationId = "opt1",
            UserId = userId,
            Successful = true,
            Explanation = "Existing optimization",
            Confidence = 0.85
        };
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(existingOptimization))
                   .Returns(expectedResult);
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeTrue();
        result.OptimizationId.Should().Be("opt1");
    }
    
    [Fact]
    public async Task OptimizePortfolioAsync_PortfolioNotFound_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "nonexistent";
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Portfolio not found");
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
    }
    
    [Fact]
    public async Task OptimizePortfolioAsync_EmptyPortfolio_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Test Portfolio",
            PortfolioStocks = new List<PortfolioStock>() // Empty portfolio
        };
        
        var portfolios = new List<Portfolio> { portfolio };
        SeedDatabase(portfolios: portfolios);
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No symbols found");
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
    }
    
    [Fact]
    public async Task OptimizePortfolioAsync_Success_CreatesOptimization()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Test Portfolio",
            PortfolioStocks = new List<PortfolioStock>
            {
                new() { 
                    Symbol = "AAPL", 
                    Quantity = 10,
                    PortfolioId = portfolioId,
                    CurrentTotalValue = 1500.0m,
                    TotalBaseValue = 1400.0m,
                    PercentageChange = 7.14m,
                    LastUpdated = DateTime.UtcNow,
                    Portfolio = null!
                }
            }
        };
        
        var portfolios = new List<Portfolio> { portfolio };
        SeedDatabase(portfolios: portfolios);
        
        var gaiaResult = new PortfolioOptimizationResult
        {
            Explanation = "Optimization complete",
            Confidence = 0.9,
            Metrics = new PortfolioMetrics
            {
                SharpeRatio = 1.5,
                MeanReturn = 0.12,
                Variance = 0.05,
                ExpectedReturn = 0.15
            },
            Recommendations = new List<PortfolioRecommendation>
            {
                new() { Symbol = "AAPL", Action = "Hold", CurrentQuantity = 10, TargetQuantity = 12 }
            }
        };
        
        _gaiaServiceMock.Setup(g => g.GetModelVersionAsync()).ReturnsAsync("v1.0");
        _gaiaServiceMock.Setup(g => g.OptimizePortfolioAsync(portfolioId)).ReturnsAsync(gaiaResult);
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns(new PortfolioOptimizationResult { Successful = true });
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeTrue();
        
        var optimizationInDb = await _dbContext.PortfolioOptimizations.FirstAsync();
        optimizationInDb.Should().NotBeNull();
        optimizationInDb.Status.Should().Be(PortfolioOptimizationStatus.Created);
        optimizationInDb.Confidence.Should().Be(0.9);
    }
    
    [Fact]
    public async Task OptimizePortfolioAsync_GaiaServiceFails_UpdatesStatusToFailed()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Test Portfolio",
            PortfolioStocks = new List<PortfolioStock>
            {
                new() { 
                    Symbol = "AAPL", 
                    Quantity = 10,
                    PortfolioId = portfolioId,
                    CurrentTotalValue = 1500.0m,
                    TotalBaseValue = 1400.0m,
                    PercentageChange = 7.14m,
                    LastUpdated = DateTime.UtcNow,
                    Portfolio = null!
                }
            }
        };
        
        var portfolios = new List<Portfolio> { portfolio };
        SeedDatabase(portfolios: portfolios);
        
        _gaiaServiceMock.Setup(g => g.GetModelVersionAsync()).ReturnsAsync("v1.0");
        _gaiaServiceMock.Setup(g => g.OptimizePortfolioAsync(portfolioId))
                        .ThrowsAsync(new Exception("Gaia service error"));
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Error optimizing portfolio");
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
    }
    
    #endregion

    #region ApplyOptimizationRecommendationAsync

    [Fact]
    public async Task ApplyOptimizationRecommendationAsync_OptimizationNotFound_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "nonexistent";
        
        // Act
        var result = await _sut.ApplyOptimizationRecommendationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendationAsync_AlreadyApplied_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = "portfolio123",
            Status = PortfolioOptimizationStatus.Created,
            IsApplied = true
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization });
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(optimization))
                   .Returns(new PortfolioOptimizationResult { Successful = false });
        
        // Act
        var result = await _sut.ApplyOptimizationRecommendationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already been applied");
    }

    [Fact]
    public async Task ApplyOptimizationRecommendationAsync_WrongStatus_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = "portfolio123",
            Status = PortfolioOptimizationStatus.Applied,
            IsApplied = false
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization });
        
        // Act
        var result = await _sut.ApplyOptimizationRecommendationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot apply optimization with status");
        result.Status.Should().Be(PortfolioOptimizationStatus.Applied);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendationAsync_PortfolioNotFound_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = "nonexistent",
            Status = PortfolioOptimizationStatus.Created,
            IsApplied = false
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization });
        
        // Act
        var result = await _sut.ApplyOptimizationRecommendationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Portfolio");
        result.ErrorMessage.Should().Contain("not found");
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendationAsync_Success_AppliesRecommendations()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        var optimizationId = "opt123";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Test Portfolio",
            PortfolioStocks = new List<PortfolioStock>
            {
                new() { 
                    Symbol = "AAPL", 
                    Quantity = 10,
                    PortfolioId = portfolioId,
                    CurrentTotalValue = 1500.0m,
                    TotalBaseValue = 1400.0m,
                    PercentageChange = 7.14m,
                    LastUpdated = DateTime.UtcNow,
                    Portfolio = null!
                }
            }
        };
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = portfolioId,
            Status = PortfolioOptimizationStatus.Created,
            IsApplied = false,
            Recommendations = new List<PortfolioOptimizationRecommendation>
            {
                new()
                {
                    Symbol = "AAPL",
                    CurrentQuantity = 10,
                    TargetQuantity = 15,
                    Action = "Buy"
                }
            }
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization }, new List<Portfolio> { portfolio });
        
        var transaction = new Transaction 
        { 
            Id = "trans1", 
            Status = TransactionStatus.Succeeded,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.0m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            TriggeredBy = AvailableTransactionTriggers.System
        };
        
        _databaseServiceMock.Setup(d => d.GetTransactionByIdAsync("trans1"))
                           .ReturnsAsync(transaction);
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns(new PortfolioOptimizationResult { Successful = true });
        
        // Act
        var result = await _sut.ApplyOptimizationRecommendationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeTrue();
        
        var updatedOptimization = await _dbContext.PortfolioOptimizations.FirstAsync();
        updatedOptimization.IsApplied.Should().BeTrue();
        updatedOptimization.Status.Should().Be(PortfolioOptimizationStatus.InProgress);
    }

    #endregion

    #region CancelOptimizationAsync

    [Fact]
    public async Task CancelOptimizationAsync_OptimizationNotFound_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "nonexistent";
        
        // Act
        var result = await _sut.CancelOptimizationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
    }

    [Fact]
    public async Task CancelOptimizationAsync_CannotCancel_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = "portfolio123",
            Status = PortfolioOptimizationStatus.Applied
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization });
        
        // Act
        var result = await _sut.CancelOptimizationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot cancel");
        result.Status.Should().Be(PortfolioOptimizationStatus.Applied);
    }

    [Fact]
    public async Task CancelOptimizationAsync_Success_CancelsOptimization()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = "portfolio123",
            Status = PortfolioOptimizationStatus.InProgress,
            Explanation = "Original explanation"
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization });
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns(new PortfolioOptimizationResult { Successful = true });
        
        // Act
        var result = await _sut.CancelOptimizationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeTrue();
        
        var updatedOptimization = await _dbContext.PortfolioOptimizations.FirstAsync();
        updatedOptimization.Status.Should().Be(PortfolioOptimizationStatus.Canceled);
        updatedOptimization.Explanation.Should().Contain("Canceled by user");
    }

    [Fact]
    public async Task CancelOptimizationAsync_WithTransactions_CancelsTransactions()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt123";
        
        var optimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = "portfolio123",
            Status = PortfolioOptimizationStatus.InProgress,
            IsApplied = true,
            TransactionIds = new List<string> { "trans1", "trans2" },
            Explanation = "Original explanation"
        };
        
        SeedDatabase(new List<PortfolioOptimization> { optimization });
        
        _databaseServiceMock.Setup(d => d.CancelTransactionAsync("trans1")).Returns(Task.CompletedTask);
        _databaseServiceMock.Setup(d => d.CancelTransactionAsync("trans2")).Returns(Task.CompletedTask);
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns(new PortfolioOptimizationResult { Successful = true });
        
        // Act
        var result = await _sut.CancelOptimizationAsync(userId, optimizationId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeTrue();
        
        _databaseServiceMock.Verify(d => d.CancelTransactionAsync("trans1"), Times.Once);
        _databaseServiceMock.Verify(d => d.CancelTransactionAsync("trans2"), Times.Once);
    }

    #endregion

    #region GetOptimizationHistoryAsync

    [Fact]
    public async Task GetOptimizationHistoryAsync_PortfolioNotFound_ReturnsFailedResult()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "nonexistent";
        
        // Act
        var result = await _sut.GetOptimizationHistoryAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var failedResult = result.First();
        failedResult.Successful.Should().BeFalse();
        failedResult.ErrorMessage.Should().Contain("Portfolio not found");
    }

    [Fact]
    public async Task GetOptimizationHistoryAsync_Success_ReturnsHistory()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Test Portfolio"
        };
        
        var optimizations = new List<PortfolioOptimization>
        {
            new()
            {
                Id = "opt1",
                UserId = userId,
                PortfolioId = portfolioId,
                Status = PortfolioOptimizationStatus.Applied,
                Timestamp = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = "opt2",
                UserId = userId,
                PortfolioId = portfolioId,
                Status = PortfolioOptimizationStatus.Failed,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        };
        
        SeedDatabase(optimizations, new List<Portfolio> { portfolio });
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns<PortfolioOptimization>(opt => new PortfolioOptimizationResult 
                   { 
                       OptimizationId = opt.Id,
                       Status = opt.Status,
                       Successful = opt.Status != PortfolioOptimizationStatus.Failed
                   });
        
        // Act
        var result = await _sut.GetOptimizationHistoryAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().OptimizationId.Should().Be("opt1"); // Most recent first
        result.First().Successful.Should().BeTrue();
        result.Last().OptimizationId.Should().Be("opt2");
        result.Last().Successful.Should().BeFalse();
    }

    [Fact]
    public async Task GetOptimizationHistoryAsync_WithDateRange_FiltersResults()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow.AddDays(-1);
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Test Portfolio"
        };
        
        var optimizations = new List<PortfolioOptimization>
        {
            new()
            {
                Id = "opt1",
                UserId = userId,
                PortfolioId = portfolioId,
                Status = PortfolioOptimizationStatus.Applied,
                Timestamp = DateTime.UtcNow.AddDays(-2) // Within range
            },
            new()
            {
                Id = "opt2",
                UserId = userId,
                PortfolioId = portfolioId,
                Status = PortfolioOptimizationStatus.Applied,
                Timestamp = DateTime.UtcNow.AddDays(-10) // Outside range
            }
        };
        
        SeedDatabase(optimizations, new List<Portfolio> { portfolio });
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns<PortfolioOptimization>(opt => new PortfolioOptimizationResult 
                   { 
                       OptimizationId = opt.Id,
                       Status = opt.Status,
                       Successful = true
                   });
        
        // Act
        var result = await _sut.GetOptimizationHistoryAsync(userId, portfolioId, startDate, endDate);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().OptimizationId.Should().Be("opt1");
    }

    #endregion

    #region GetOptimizationsByPortfolioAsync

    [Fact]
    public async Task GetOptimizationsByPortfolioAsync_ReturnsAllOptimizations()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new()
            {
                Id = "opt1",
                UserId = userId,
                PortfolioId = portfolioId,
                Status = PortfolioOptimizationStatus.Applied,
                Timestamp = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = "opt2",
                UserId = userId,
                PortfolioId = portfolioId,
                Status = PortfolioOptimizationStatus.Created,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        };
        
        SeedDatabase(optimizations);
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
                   .Returns<PortfolioOptimization>(opt => new PortfolioOptimizationResult 
                   { 
                       OptimizationId = opt.Id,
                       Status = opt.Status,
                       Successful = opt.Status != PortfolioOptimizationStatus.Failed
                   });
        
        // Act
        var result = await _sut.GetOptimizationsByPortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().OptimizationId.Should().Be("opt1"); // Most recent first
    }

    [Fact]
    public async Task GetOptimizationsByPortfolioAsync_Exception_ReturnsEmptyList()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        
        // Dispose the context to cause an exception
        _dbContext.Dispose();
        
        // Act
        var result = await _sut.GetOptimizationsByPortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region EnsureCompletionOfAllInProgressOptimizationsAsync

    [Fact]
    public async Task EnsureCompletionOfAllInProgressOptimizationsAsync_ProcessesInProgressOptimizations()
    {
        // Arrange
        var optimizations = new List<PortfolioOptimization>
        {
            new()
            {
                Id = "opt1",
                UserId = "user123",
                PortfolioId = "portfolio123",
                Status = PortfolioOptimizationStatus.InProgress,
                TransactionIds = new List<string> { "trans1" }
            },
            new()
            {
                Id = "opt2",
                UserId = "user123",
                PortfolioId = "portfolio123",
                Status = PortfolioOptimizationStatus.Applied // Should be ignored
            }
        };
        
        SeedDatabase(optimizations);
        
        var transaction = new Transaction 
        { 
            Id = "trans1", 
            Status = TransactionStatus.Succeeded,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.0m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            TriggeredBy = AvailableTransactionTriggers.System
        };
        
        _databaseServiceMock.Setup(d => d.GetTransactionByIdAsync("trans1"))
                           .ReturnsAsync(transaction);
        
        // Act
        await _sut.EnsureCompletionOfAllInProgressOptimizationsAsync();
        
        // Assert
        _databaseServiceMock.Verify(d => d.GetTransactionByIdAsync("trans1"), Times.Once);
    }

    #endregion
} 