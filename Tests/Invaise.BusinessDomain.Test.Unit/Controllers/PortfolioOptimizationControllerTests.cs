using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class PortfolioOptimizationControllerTests : TestBase
{
    private readonly Mock<IPortfolioOptimizationService> _portfolioOptimizationServiceMock;
    private readonly Mock<Serilog.ILogger> _serilogLoggerMock;
    private readonly PortfolioOptimizationController _controller;
    private readonly User _testUser;

    public PortfolioOptimizationControllerTests()
    {
        _portfolioOptimizationServiceMock = new Mock<IPortfolioOptimizationService>();
        _serilogLoggerMock = new Mock<Serilog.ILogger>();
        _controller = new PortfolioOptimizationController(_portfolioOptimizationServiceMock.Object, _serilogLoggerMock.Object);
        
        // Setup controller context
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Create test user
        _testUser = new User
        {
            Id = "user-id",
            DisplayName = "Test User",
            Email = "test@example.com",
            Role = "User"
        };
        
        // Set user in HttpContext
        _controller.HttpContext.Items["User"] = _testUser;
    }

    [Fact]
    public async Task OptimizePortfolio_Success_ReturnsOkWithResult()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.HasOngoingOptimizationAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(false);
            
        _portfolioOptimizationServiceMock.Setup(s => s.GetRemainingCoolOffTime(_testUser.Id, portfolioId))
            .ReturnsAsync(TimeSpan.Zero);
            
        var optimizationResult = new PortfolioOptimizationResult
        {
            OptimizationId = "optimization-id",
            UserId = _testUser.Id,
            Timestamp = DateTime.UtcNow,
            Status = PortfolioOptimizationStatus.Created
        };
        
        _portfolioOptimizationServiceMock.Setup(s => s.OptimizePortfolioAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(optimizationResult);

        // Act
        var result = await _controller.OptimizePortfolio(portfolioId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PortfolioOptimizationResult>(okResult.Value);
        Assert.Equal(optimizationResult.OptimizationId, returnedResult.OptimizationId);
        Assert.Equal(optimizationResult.UserId, returnedResult.UserId);
    }

    [Fact]
    public async Task OptimizePortfolio_OngoingOptimization_ReturnsConflict()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.HasOngoingOptimizationAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(true);
            
        var existingOptimizations = new List<PortfolioOptimizationResult>
        {
            new PortfolioOptimizationResult
            {
                OptimizationId = "optimization-id",
                UserId = _testUser.Id,
                Timestamp = DateTime.UtcNow,
                Status = PortfolioOptimizationStatus.InProgress
            }
        };
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationsByPortfolioAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(existingOptimizations);

        // Act
        var result = await _controller.OptimizePortfolio(portfolioId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("optimization", conflictResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task OptimizePortfolio_WithinCoolOffPeriod_ReturnsConflict()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.HasOngoingOptimizationAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(false);
            
        _portfolioOptimizationServiceMock.Setup(s => s.GetRemainingCoolOffTime(_testUser.Id, portfolioId))
            .ReturnsAsync(TimeSpan.FromHours(2));

        // Act
        var result = await _controller.OptimizePortfolio(portfolioId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("wait", conflictResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task OptimizePortfolio_PortfolioNotFound_ReturnsNotFound()
    {
        // Arrange
        string portfolioId = "non-existent-portfolio";
        
        _portfolioOptimizationServiceMock.Setup(s => s.HasOngoingOptimizationAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(false);
            
        _portfolioOptimizationServiceMock.Setup(s => s.GetRemainingCoolOffTime(_testUser.Id, portfolioId))
            .ReturnsAsync(TimeSpan.Zero);
            
        _portfolioOptimizationServiceMock.Setup(s => s.OptimizePortfolioAsync(_testUser.Id, portfolioId))
            .ThrowsAsync(new KeyNotFoundException("Portfolio not found"));

        // Act
        var result = await _controller.OptimizePortfolio(portfolioId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task OptimizePortfolio_Exception_ReturnsBadRequest()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.HasOngoingOptimizationAsync(_testUser.Id, portfolioId))
            .ReturnsAsync(false);
            
        _portfolioOptimizationServiceMock.Setup(s => s.GetRemainingCoolOffTime(_testUser.Id, portfolioId))
            .ReturnsAsync(TimeSpan.Zero);
            
        _portfolioOptimizationServiceMock.Setup(s => s.OptimizePortfolioAsync(_testUser.Id, portfolioId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.OptimizePortfolio(portfolioId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error optimizing portfolio", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetOptimizationHistory_Success_ReturnsOkWithResults()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        
        var optimizationResults = new List<PortfolioOptimizationResult>
        {
            new PortfolioOptimizationResult
            {
                OptimizationId = "optimization-1",
                UserId = _testUser.Id,
                Timestamp = DateTime.UtcNow.AddDays(-7),
                Status = PortfolioOptimizationStatus.Applied
            },
            new PortfolioOptimizationResult
            {
                OptimizationId = "optimization-2",
                UserId = _testUser.Id,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Status = PortfolioOptimizationStatus.Created
            }
        };
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationHistoryAsync(
                _testUser.Id, portfolioId, startDate, endDate))
            .ReturnsAsync(optimizationResults);

        // Act
        var result = await _controller.GetOptimizationHistory(portfolioId, startDate, endDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResults = Assert.IsAssignableFrom<IEnumerable<PortfolioOptimizationResult>>(okResult.Value);
        Assert.Equal(2, returnedResults.Count());
    }

    [Fact]
    public async Task GetOptimizationHistory_Exception_ReturnsBadRequest()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationHistoryAsync(
                _testUser.Id, portfolioId, null, null))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetOptimizationHistory(portfolioId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error getting optimization history", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_Success_ReturnsOk()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.Created);
            
        var result = new PortfolioOptimizationResult
        {
            OptimizationId = optimizationId,
            UserId = _testUser.Id,
            Successful = true,
            Status = PortfolioOptimizationStatus.Applied
        };
            
        _portfolioOptimizationServiceMock.Setup(s => s.ApplyOptimizationRecommendationAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.Contains("applied successfully", okResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_InvalidOptimizationId_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "";

        // Act
        var result = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Optimization ID is required", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_InProgressStatus_ReturnsConflict()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.InProgress);

        // Act
        var result = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("still in progress", conflictResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_AlreadyAppliedStatus_ReturnsConflict()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.Applied);

        // Act
        var result = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("already been applied", conflictResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_CanceledStatus_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.Canceled);

        // Act
        var result = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("canceled", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_FailedStatus_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.Failed);

        // Act
        var result = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("failed", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_ServiceReturnsFalse_ReturnsNotFound()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.Created);
            
        var result = new PortfolioOptimizationResult
        {
            OptimizationId = optimizationId,
            UserId = _testUser.Id,
            Successful = false,
            ErrorMessage = "Optimization not found",
            Status = PortfolioOptimizationStatus.Failed
        };
            
        _portfolioOptimizationServiceMock.Setup(s => s.ApplyOptimizationRecommendationAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task ApplyOptimizationRecommendation_Exception_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.GetOptimizationStatusAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(PortfolioOptimizationStatus.Created);
            
        _portfolioOptimizationServiceMock.Setup(s => s.ApplyOptimizationRecommendationAsync(_testUser.Id, optimizationId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.ApplyOptimizationRecommendation(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error applying optimization", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CancelOptimization_Success_ReturnsOk()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        var result = new PortfolioOptimizationResult
        {
            OptimizationId = optimizationId,
            UserId = _testUser.Id,
            Successful = true,
            Status = PortfolioOptimizationStatus.Canceled
        };
            
        _portfolioOptimizationServiceMock.Setup(s => s.CancelOptimizationAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CancelOptimization(optimizationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.Contains("canceled successfully", okResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CancelOptimization_InvalidOptimizationId_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "";

        // Act
        var result = await _controller.CancelOptimization(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Optimization ID is required", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CancelOptimization_OptimizationNotFound_ReturnsNotFound()
    {
        // Arrange
        string optimizationId = "non-existent-optimization";
        
        var result = new PortfolioOptimizationResult
        {
            OptimizationId = optimizationId,
            UserId = _testUser.Id,
            Successful = false,
            ErrorMessage = "Optimization not found",
            Status = PortfolioOptimizationStatus.Failed
        };
            
        _portfolioOptimizationServiceMock.Setup(s => s.CancelOptimizationAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CancelOptimization(optimizationId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task CancelOptimization_ServiceReturnsFalse_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        var result = new PortfolioOptimizationResult
        {
            OptimizationId = optimizationId,
            UserId = _testUser.Id,
            Successful = false,
            ErrorMessage = "Cannot cancel optimization in current state",
            Status = PortfolioOptimizationStatus.Failed
        };
            
        _portfolioOptimizationServiceMock.Setup(s => s.CancelOptimizationAsync(_testUser.Id, optimizationId))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CancelOptimization(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Contains("Cannot cancel optimization", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CancelOptimization_Exception_ReturnsBadRequest()
    {
        // Arrange
        string optimizationId = "optimization-id";
        
        _portfolioOptimizationServiceMock.Setup(s => s.CancelOptimizationAsync(_testUser.Id, optimizationId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.CancelOptimization(optimizationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error canceling optimization", badRequestResult.Value?.ToString() ?? string.Empty);
    }
} 