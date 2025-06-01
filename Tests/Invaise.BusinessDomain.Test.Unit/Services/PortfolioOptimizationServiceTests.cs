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

namespace Invaise.BusinessDomain.Test.Unit.Services;

// NOTE: This test file is skipped because we can't mock DbContext properties properly
// The DbContext properties are not virtual or overridable, which makes it difficult to mock them
// To properly test this service, we would need to:
// 1. Use a different approach for mocking DbContext
// 2. Or use an in-memory database for integration testing
// 3. Or refactor the service to accept DbSet dependencies directly
public class PortfolioOptimizationServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly PortfolioOptimizationService _sut;
    private readonly Mock<IDatabaseService> _databaseServiceMock;
    private readonly Mock<IGaiaService> _gaiaServiceMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly DbContextOptions<InvaiseDbContext> _options;

    public PortfolioOptimizationServiceTests()
    {
        // Setup in-memory database
        _options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: $"PortfolioOptimizationServiceTestDb_{Guid.NewGuid()}")
            .Options;

        // Create DbContext with in-memory database
        _dbContext = new InvaiseDbContext(_options);
        
        // Create mocks
        _databaseServiceMock = new Mock<IDatabaseService>();
        _gaiaServiceMock = new Mock<IGaiaService>();
        _transactionServiceMock = new Mock<ITransactionService>();
        _loggerMock = new Mock<Serilog.ILogger>();
        _mapperMock = new Mock<IMapper>();
        
        // Create service with mocks and real DbContext
        _sut = new PortfolioOptimizationService(
            _databaseServiceMock.Object,
            _dbContext,
            _gaiaServiceMock.Object,
            _transactionServiceMock.Object,
            _loggerMock.Object,
            _mapperMock.Object);
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
        
        var appliedDate = DateTime.UtcNow.AddHours(-1); // 1 hour ago
        
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
        // Use separate assertions rather than BeInRange which isn't available for TimeSpan
        result.Should().BeGreaterThan(TimeSpan.FromHours(GaiaConstants.OptimizationCoolOffPeriod - 1.1));
        result.Should().BeLessThan(TimeSpan.FromHours(GaiaConstants.OptimizationCoolOffPeriod - 0.9));
    }
    
    #endregion
    
    #region GetOptimizationStatusAsync
    
    [Fact]
    public async Task GetOptimizationStatusAsync_OptimizationExists_ReturnsStatus()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "opt1";
        var expectedStatus = PortfolioOptimizationStatus.InProgress;
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() 
            { 
                Id = optimizationId, 
                UserId = userId, 
                Status = expectedStatus 
            }
        };
        
        SeedDatabase(optimizations);
        
        // Act
        var result = await _sut.GetOptimizationStatusAsync(userId, optimizationId);
        
        // Assert
        result.Should().Be(expectedStatus);
    }
    
    [Fact]
    public async Task GetOptimizationStatusAsync_OptimizationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = "user123";
        var optimizationId = "nonexistent";
        
        var optimizations = new List<PortfolioOptimization>
        {
            new() { Id = "other", UserId = userId }
        };
        
        SeedDatabase(optimizations);
        
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _sut.GetOptimizationStatusAsync(userId, optimizationId));
    }
    
    #endregion
    
    #region OptimizePortfolioAsync
    
    [Fact]
    public async Task OptimizePortfolioAsync_ExistingOptimization_ReturnsExistingOptimization()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "portfolio123";
        var optimizationId = "opt1";
        
        var existingOptimization = new PortfolioOptimization
        {
            Id = optimizationId,
            UserId = userId,
            PortfolioId = portfolioId,
            Status = PortfolioOptimizationStatus.Created,
            Recommendations = new List<PortfolioOptimizationRecommendation>()
        };
        
        var optimizations = new List<PortfolioOptimization> { existingOptimization };
        SeedDatabase(optimizations);
        
        var expectedResult = new PortfolioOptimizationResult
        {
            OptimizationId = optimizationId,
            UserId = userId,
            Successful = true
        };
        
        _mapperMock.Setup(m => m.Map<PortfolioOptimizationResult>(It.IsAny<PortfolioOptimization>()))
            .Returns(expectedResult);
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeTrue();
        result.OptimizationId.Should().Be(optimizationId);
    }
    
    [Fact]
    public async Task OptimizePortfolioAsync_PortfolioNotFound_ReturnsFailed()
    {
        // Arrange
        var userId = "user123";
        var portfolioId = "nonexistent";
        
        // Empty database, no portfolios exist
        
        // Act
        var result = await _sut.OptimizePortfolioAsync(userId, portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Successful.Should().BeFalse();
        result.Status.Should().Be(PortfolioOptimizationStatus.Failed);
        result.ErrorMessage.Should().Contain("Portfolio not found");
    }
    
    #endregion
} 