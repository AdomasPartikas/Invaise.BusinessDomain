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
            Recommendations = []
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