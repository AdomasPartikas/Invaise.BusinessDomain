using Invaise.BusinessDomain.API.ApolloAPIClient;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using Invaise.BusinessDomain.Test.Unit.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class ApolloServiceTests : TestBase
{
    private readonly Mock<IHealthApolloClient> _mockHealthClient;
    private readonly Mock<IPredictApolloClient> _mockPredictClient;
    private readonly Mock<IInfoApolloClient> _mockInfoClient;
    private readonly Mock<ITrainApolloClient> _mockTrainClient;
    private readonly Mock<IStatusApolloClient> _mockStatusClient;
    private readonly Mock<Serilog.ILogger> _mockLogger;
    private readonly ApolloService _service;

    public ApolloServiceTests()
    {
        _mockHealthClient = new Mock<IHealthApolloClient>();
        _mockPredictClient = new Mock<IPredictApolloClient>();
        _mockInfoClient = new Mock<IInfoApolloClient>();
        _mockTrainClient = new Mock<ITrainApolloClient>();
        _mockStatusClient = new Mock<IStatusApolloClient>();
        _mockLogger = new Mock<Serilog.ILogger>();

        _service = new ApolloService(
            _mockHealthClient.Object,
            _mockPredictClient.Object,
            _mockInfoClient.Object,
            _mockTrainClient.Object,
            _mockStatusClient.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsTrue_WhenStatusIsOk()
    {
        // Arrange
        var healthResponse = new HealthResponse 
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
        var healthResponse = new HealthResponse 
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
            .ReturnsAsync((HealthResponse)null!);

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
    public async Task GetHeatPredictionAsync_ReturnsHeat_WhenResponseIsValid()
    {
        // Arrange
        var symbol = "AAPL";
        var predictionPeriod = ApolloConstants.ApolloPredictionPeriod;
        var lookbackPeriod = ApolloConstants.ApolloLookbackPeriod;

        var predictionResponse = new PredictResponse
        {
            Symbol = symbol,
            Heat_score = 0.75,
            Confidence = 0.85,
            Direction = "up",
            Explanation = "Positive sentiment analysis",
            Predicted_next_close = 150.5
        };
        
        _mockPredictClient
            .Setup(client => client.GetAsync(symbol, predictionPeriod, lookbackPeriod))
            .ReturnsAsync(predictionResponse);

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        var (heat, prediction) = result.Value;
        
        heat.Should().NotBeNull();
        heat.Symbol.Should().Be(symbol);
        heat.HeatScore.Should().Be(0.75);
        heat.Score.Should().Be(75);
        heat.Confidence.Should().Be(85);
        heat.Direction.Should().Be("up");
        heat.Explanation.Should().Be("Positive sentiment analysis");

        prediction.Should().Be(150.5);
        
        _mockPredictClient.Verify(client => client.GetAsync(
            symbol, 
            predictionPeriod,
            lookbackPeriod), 
            Times.Once);
    }

    [Fact]
    public async Task GetHeatPredictionAsync_HandlesNullDirection()
    {
        // Arrange
        var symbol = "AAPL";
        var predictionPeriod = ApolloConstants.ApolloPredictionPeriod;
        var lookbackPeriod = ApolloConstants.ApolloLookbackPeriod;

        var predictionResponse = new PredictResponse
        {
            Symbol = symbol,
            Heat_score = 0.75,
            Confidence = 0.85,
            Direction = null,
            Explanation = "Positive sentiment analysis",
            Predicted_next_close = 150.5
        };
        
        _mockPredictClient
            .Setup(client => client.GetAsync(symbol, predictionPeriod, lookbackPeriod))
            .ReturnsAsync(predictionResponse);

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        var (heat, prediction) = result.Value;
        heat.Direction.Should().Be("neutral"); // Default value
    }

    [Fact]
    public async Task GetHeatPredictionAsync_HandlesNullExplanation()
    {
        // Arrange
        var symbol = "AAPL";
        var predictionPeriod = ApolloConstants.ApolloPredictionPeriod;
        var lookbackPeriod = ApolloConstants.ApolloLookbackPeriod;

        var predictionResponse = new PredictResponse
        {
            Symbol = symbol,
            Heat_score = 0.75,
            Confidence = 0.85,
            Direction = "up",
            Explanation = null,
            Predicted_next_close = 150.5
        };
        
        _mockPredictClient
            .Setup(client => client.GetAsync(symbol, predictionPeriod, lookbackPeriod))
            .ReturnsAsync(predictionResponse);

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        var (heat, prediction) = result.Value;
        heat.Explanation.Should().Be("No explanation provided"); // Default value in ApolloService
    }

    [Fact]
    public async Task GetHeatPredictionAsync_ReturnsNull_WhenResponseIsNull()
    {
        // Arrange
        var symbol = "AAPL";
        var predictionPeriod = ApolloConstants.ApolloPredictionPeriod;
        var lookbackPeriod = ApolloConstants.ApolloLookbackPeriod;
        
        _mockPredictClient
            .Setup(client => client.GetAsync(symbol, predictionPeriod, lookbackPeriod))
            .ReturnsAsync((PredictResponse)null!);

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol);

        // Assert
        result.Should().BeNull();
        _mockPredictClient.Verify(client => client.GetAsync(
            symbol, 
            predictionPeriod,
            lookbackPeriod), 
            Times.Once);
    }

    [Fact]
    public async Task GetHeatPredictionAsync_ReturnsNull_WhenExceptionThrown()
    {
        // Arrange
        var symbol = "AAPL";
        var predictionPeriod = ApolloConstants.ApolloPredictionPeriod;
        var lookbackPeriod = ApolloConstants.ApolloLookbackPeriod;
        
        _mockPredictClient
            .Setup(client => client.GetAsync(symbol, predictionPeriod, lookbackPeriod))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.GetHeatPredictionAsync(symbol);

        // Assert
        result.Should().BeNull();
        _mockPredictClient.Verify(client => client.GetAsync(
            symbol, 
            predictionPeriod,
            lookbackPeriod), 
            Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestRetrainingAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        var trainingResponse = new TrainingStartResponse 
        { 
            Success = true,
            Message = "Training started successfully"
        };
        
        _mockTrainClient
            .Setup(client => client.PostAsync())
            .ReturnsAsync(trainingResponse);

        // Act
        var result = await _service.RequestRetrainingAsync();

        // Assert
        result.Should().BeTrue();
        _mockTrainClient.Verify(client => client.PostAsync(), Times.Once);
    }

    [Fact]
    public async Task RequestRetrainingAsync_ReturnsFalse_WhenUnsuccessful()
    {
        // Arrange
        var trainingResponse = new TrainingStartResponse 
        { 
            Success = false,
            Message = "Training failed to start"
        };
        
        _mockTrainClient
            .Setup(client => client.PostAsync())
            .ReturnsAsync(trainingResponse);

        // Act
        var result = await _service.RequestRetrainingAsync();

        // Assert
        result.Should().BeFalse();
        _mockTrainClient.Verify(client => client.PostAsync(), Times.Once);
    }

    [Fact]
    public async Task RequestRetrainingAsync_ReturnsFalse_WhenExceptionThrown()
    {
        // Arrange
        _mockTrainClient
            .Setup(client => client.PostAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.RequestRetrainingAsync();

        // Assert
        result.Should().BeFalse();
        _mockTrainClient.Verify(client => client.PostAsync(), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task IsTrainingAsync_ReturnsTrue_WhenIsTrainingIsTrue()
    {
        // Arrange
        var statusResponse = new TrainingStatusResponse 
        { 
            Is_training = true
        };
        
        _mockStatusClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(statusResponse);

        // Act
        var result = await _service.IsTrainingAsync();

        // Assert
        result.Should().BeTrue();
        _mockStatusClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task IsTrainingAsync_ReturnsFalse_WhenIsTrainingIsFalse()
    {
        // Arrange
        var statusResponse = new TrainingStatusResponse 
        { 
            Is_training = false
        };
        
        _mockStatusClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(statusResponse);

        // Act
        var result = await _service.IsTrainingAsync();

        // Assert
        result.Should().BeFalse();
        _mockStatusClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task IsTrainingAsync_ReturnsFalse_WhenResponseIsNull()
    {
        // Arrange
        _mockStatusClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync((TrainingStatusResponse)null!);

        // Act
        var result = await _service.IsTrainingAsync();

        // Assert
        result.Should().BeFalse();
        _mockStatusClient.Verify(client => client.GetAsync(), Times.Once);
        _mockLogger.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task IsTrainingAsync_ReturnsFalse_WhenExceptionThrown()
    {
        // Arrange
        _mockStatusClient
            .Setup(client => client.GetAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.IsTrainingAsync();

        // Assert
        result.Should().BeFalse();
        _mockStatusClient.Verify(client => client.GetAsync(), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetModelVersionAsync_ReturnsVersion_WhenResponseIsValid()
    {
        // Arrange
        var expectedVersion = "1.2.3";
        var infoResponse = new InfoResponse 
        { 
            Model_version = expectedVersion
        };
        
        _mockInfoClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(infoResponse);

        // Act
        var result = await _service.GetModelVersionAsync();

        // Assert
        result.Should().Be(expectedVersion);
        _mockInfoClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task GetModelVersionAsync_ReturnsUnknown_WhenVersionIsNull()
    {
        // Arrange
        var infoResponse = new InfoResponse 
        { 
            Model_version = null
        };
        
        _mockInfoClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(infoResponse);

        // Act
        var result = await _service.GetModelVersionAsync();

        // Assert
        result.Should().Be("unknown");
        _mockInfoClient.Verify(client => client.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task GetModelVersionAsync_ReturnsUnknown_WhenExceptionThrown()
    {
        // Arrange
        _mockInfoClient
            .Setup(client => client.GetAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.GetModelVersionAsync();

        // Assert
        result.Should().Be("unknown");
        _mockInfoClient.Verify(client => client.GetAsync(), Times.Once);
        _mockLogger.Verify(logger => logger.Error(It.IsAny<Exception>(), "Error getting Apollo model version"), Times.Once);
    }
} 