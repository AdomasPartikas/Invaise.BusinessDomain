using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.GaiaAPIClient;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Profiles;
using Invaise.BusinessDomain.API.Services;
using Invaise.BusinessDomain.Test.Unit.Utilities;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Invaise.BusinessDomain.Test.Unit.Utilities.MockResponses.GaiaAPI;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class GaiaServiceTests : TestBase
{
    private readonly Mock<IHealthGaiaClient> _mockHealthClient;
    private readonly Mock<IPredictGaiaClient> _mockPredictClient;
    private readonly Mock<IOptimizeGaiaClient> _mockOptimizeClient;
    private readonly Mock<IWeightsGaiaClient> _mockWeightsClient;
    private readonly Mock<Serilog.ILogger> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GaiaService _service;

    public GaiaServiceTests()
    {
        _mockHealthClient = new Mock<IHealthGaiaClient>();
        _mockPredictClient = new Mock<IPredictGaiaClient>();
        _mockOptimizeClient = new Mock<IOptimizeGaiaClient>();
        _mockWeightsClient = new Mock<IWeightsGaiaClient>();
        _mockLogger = new Mock<Serilog.ILogger>();
        _mockMapper = new Mock<IMapper>();

        _service = new GaiaService(
            _mockHealthClient.Object,
            _mockPredictClient.Object,
            _mockOptimizeClient.Object,
            _mockWeightsClient.Object,
            _mockLogger.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsTrue_WhenStatusIsOk()
    {
        // Arrange
        var healthResponse = new API.GaiaAPIClient.HealthResponse 
        { 
            Status = "ok"
        };
        
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(healthResponse);

        // Act
        var result = await _service.CheckHealthAsync();

        // Assert
        result.Should().BeTrue();
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsFalse_WhenStatusIsNotOk()
    {
        // Arrange
        var healthResponse = new API.GaiaAPIClient.HealthResponse 
        { 
            Status = "error"
        };
        
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(healthResponse);

        // Act
        var result = await _service.CheckHealthAsync();

        // Assert
        result.Should().BeFalse();
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsFalse_WhenResponseIsNull()
    {
        // Arrange
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync((API.GaiaAPIClient.HealthResponse)null!);

        // Act
        var result = await _service.CheckHealthAsync();

        // Assert
        result.Should().BeFalse();
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsFalse_WhenExceptionThrown()
    {
        // Arrange
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.CheckHealthAsync();

        // Assert
        result.Should().BeFalse();
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task GetModelVersionAsync_ReturnsVersion_WhenResponseIsValid()
    {
        // Arrange
        var expectedVersion = "1.2.3";
        var healthResponse = new API.GaiaAPIClient.HealthResponse 
        { 
            Status = "ok",
            Version = expectedVersion
        };
        
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(healthResponse);

        // Act
        var result = await _service.GetModelVersionAsync();

        // Assert
        result.Should().Be(expectedVersion);
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task GetModelVersionAsync_ReturnsUnknown_WhenResponseIsNull()
    {
        // Arrange
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync((API.GaiaAPIClient.HealthResponse)null!);

        // Act
        var result = await _service.GetModelVersionAsync();

        // Assert
        result.Should().Be("unknown");
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task GetModelVersionAsync_ReturnsUnknown_WhenExceptionThrown()
    {
        // Arrange
        _mockHealthClient
            .Setup(client => client.GetAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.GetModelVersionAsync();

        // Assert
        result.Should().Be("unknown");
        _mockHealthClient.Verify(client => client.GetAsync(), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), "Error getting Gaia model version"), Times.Once);
    }

    [Fact]
    public async Task GetHeatPredictionAsync_ReturnsHeat_WhenResponseIsValid()
    {
        // Arrange
        var symbol = "AAPL";
        var portfolioId = "portfolio123";
        var expectedDate = DateTime.UtcNow.AddDays(1);
        var expectedPrediction = 150.5;
        
        var heatData = new API.GaiaAPIClient.HeatData
        {
            Heat_score = 0.75,
            Confidence = 0.85,
            Direction = "up",
            Timestamp = DateTime.UtcNow.ToString("o"),
            Source = "gaia",
            Explanation = "Positive market trends",
            Apollo_contribution = 0.4,
            Ignis_contribution = 0.6,
            Prediction_id = 123,
            Model_version = "1.0.0",
            Prediction_target = expectedDate.ToString("o"),
            Current_price = 145.0,
            Predicted_price = expectedPrediction
        };
        
        var predictionResponse = new API.GaiaAPIClient.PredictionResponse
        {
            Symbol = symbol,
            Combined_heat = heatData
        };
        
        var expectedHeat = new Heat
        {
            Symbol = symbol,
            Score = 75,
            Confidence = 85,
            HeatScore = 0.75,
            Direction = "up",
            Explanation = "Positive market trends",
            ApolloContribution = 0.4,
            IgnisContribution = 0.6,
            PredictionId = 123
        };
        
        _mockPredictClient
            .Setup(client => client.PostAsync(It.Is<API.GaiaAPIClient.PredictionRequest>(req => 
                req.Symbol == symbol && req.Portfolio_id == portfolioId)))
            .ReturnsAsync(predictionResponse);
            
        _mockMapper
            .Setup(mapper => mapper.Map<Heat>(It.IsAny<API.GaiaAPIClient.PredictionResponse>()))
            .Returns(expectedHeat);

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol, portfolioId);

        // Assert
        result.Should().NotBeNull();
        var (heat, targetDate, prediction) = result.Value;
        heat.Should().Be(expectedHeat);
        targetDate.Date.Should().Be(expectedDate.Date);
        prediction.Should().Be(expectedPrediction);
        
        _mockPredictClient.Verify(client => client.PostAsync(
            It.Is<API.GaiaAPIClient.PredictionRequest>(req => 
                req.Symbol == symbol && req.Portfolio_id == portfolioId)), 
            Times.Once);
        
        _mockMapper.Verify(mapper => mapper.Map<Heat>(It.IsAny<API.GaiaAPIClient.PredictionResponse>()), Times.Once);
    }

    [Fact]
    public async Task GetHeatPredictionAsync_ReturnsNull_WhenResponseIsNull()
    {
        // Arrange
        var symbol = "AAPL";
        var portfolioId = "portfolio123";
        
        _mockPredictClient
            .Setup(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.PredictionRequest>()))
            .ReturnsAsync((API.GaiaAPIClient.PredictionResponse)null!);

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol, portfolioId);

        // Assert
        result.Should().BeNull();
        _mockPredictClient.Verify(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.PredictionRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetHeatPredictionAsync_ReturnsNull_WhenExceptionThrown()
    {
        // Arrange
        var symbol = "AAPL";
        var portfolioId = "portfolio123";
        
        _mockPredictClient
            .Setup(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.PredictionRequest>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol, portfolioId);

        // Assert
        result.Should().BeNull();
        _mockPredictClient.Verify(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.PredictionRequest>()), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task OptimizePortfolioAsync_ReturnsOptimizationResult_WhenResponseIsValid()
    {
        // Arrange
        var portfolioId = "portfolio123";
        
        var optimizationResponse = new API.GaiaAPIClient.OptimizationResponse
        {
            Id = "opt123",
            PortfolioId = portfolioId,
            UserId = "user123",
            Timestamp = DateTime.UtcNow.ToString("o"),
            Explanation = "Portfolio optimized successfully",
            Confidence = 0.9,
            RiskTolerance = 0.5,
            IsApplied = false,
            ModelVersion = "1.0.0",
            SharpeRatio = 1.5,
            MeanReturn = 0.12,
            Variance = 0.05,
            ExpectedReturn = 0.15,
            Recommendations = new List<API.GaiaAPIClient.RecommendationData>
            {
                new() {
                    Symbol = "AAPL",
                    Action = "BUY",
                    CurrentQuantity = 10,
                    TargetQuantity = 15,
                    CurrentWeight = 0.2,
                    TargetWeight = 0.3,
                    Explanation = "Increase allocation due to positive outlook"
                }
            },
            SymbolsProcessed = new List<string> { "AAPL", "MSFT" },
            PortfolioStrategy = "GROWTH"
        };
        
        var expectedResult = new PortfolioOptimizationResult
        {
            UserId = "user123",
            Explanation = "Portfolio optimized successfully",
            Confidence = 0.9,
            Timestamp = DateTime.Parse(optimizationResponse.Timestamp, System.Globalization.CultureInfo.InvariantCulture)
        };
        
        _mockOptimizeClient
            .Setup(client => client.PostAsync(It.Is<API.GaiaAPIClient.OptimizationRequest>(req => 
                req.Portfolio_id == portfolioId)))
            .ReturnsAsync(optimizationResponse);
            
        _mockMapper
            .Setup(mapper => mapper.Map<PortfolioOptimizationResult>(It.IsAny<API.GaiaAPIClient.OptimizationResponse>()))
            .Returns(expectedResult);

        // Act
        var result = await _service.OptimizePortfolioAsync(portfolioId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(expectedResult.UserId);
        result.Explanation.Should().Be(expectedResult.Explanation);
        result.Confidence.Should().Be(expectedResult.Confidence);
        
        _mockOptimizeClient.Verify(client => client.PostAsync(
            It.Is<API.GaiaAPIClient.OptimizationRequest>(req => req.Portfolio_id == portfolioId)), 
            Times.Once);
        
        _mockMapper.Verify(mapper => mapper.Map<PortfolioOptimizationResult>(It.IsAny<API.GaiaAPIClient.OptimizationResponse>()), Times.Once);
    }

    [Fact]
    public async Task OptimizePortfolioAsync_ReturnsErrorResult_WhenResponseIsNull()
    {
        // Arrange
        var portfolioId = "portfolio123";
        
        _mockOptimizeClient
            .Setup(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.OptimizationRequest>()))
            .ReturnsAsync((API.GaiaAPIClient.OptimizationResponse)null!);

        // Act
        var result = await _service.OptimizePortfolioAsync(portfolioId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("unknown");
        result.Explanation.Should().Be("Failed to get optimization from Gaia");
        
        _mockOptimizeClient.Verify(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.OptimizationRequest>()), Times.Once);
    }

    [Fact]
    public async Task OptimizePortfolioAsync_ReturnsErrorResult_WhenExceptionThrown()
    {
        // Arrange
        var portfolioId = "portfolio123";
        var errorMessage = "Test exception";
        
        _mockOptimizeClient
            .Setup(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.OptimizationRequest>()))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _service.OptimizePortfolioAsync(portfolioId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("unknown");
        result.Explanation.Should().Be($"Error optimizing portfolio: {errorMessage}");
        
        _mockOptimizeClient.Verify(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.OptimizationRequest>()), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AdjustModelWeightsAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        double apolloWeight = 0.6;
        double ignisWeight = 0.4;
        
        _mockWeightsClient
            .Setup(client => client.PostAsync(It.Is<API.GaiaAPIClient.WeightAdjustRequest>(req => 
                Math.Abs(req.Apollo_weight - apolloWeight) < 0.001 && 
                Math.Abs(req.Ignis_weight - ignisWeight) < 0.001)))
            .ReturnsAsync(new object());

        // Act
        var result = await _service.AdjustModelWeightsAsync(apolloWeight, ignisWeight);

        // Assert
        result.Should().BeTrue();
        
        _mockWeightsClient.Verify(client => client.PostAsync(
            It.Is<API.GaiaAPIClient.WeightAdjustRequest>(req => 
                Math.Abs(req.Apollo_weight - apolloWeight) < 0.001 && 
                Math.Abs(req.Ignis_weight - ignisWeight) < 0.001)), 
            Times.Once);
    }

    [Fact]
    public async Task AdjustModelWeightsAsync_ReturnsFalse_WhenExceptionThrown()
    {
        // Arrange
        double apolloWeight = 0.6;
        double ignisWeight = 0.4;
        
        _mockWeightsClient
            .Setup(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.WeightAdjustRequest>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.AdjustModelWeightsAsync(apolloWeight, ignisWeight);

        // Assert
        result.Should().BeFalse();
        
        _mockWeightsClient.Verify(client => client.PostAsync(It.IsAny<API.GaiaAPIClient.WeightAdjustRequest>()), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }
} 