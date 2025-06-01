using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class ModelPredictionControllerTests : TestBase
{
    private readonly Mock<IModelPredictionService> _modelPredictionServiceMock;
    private readonly ModelPredictionController _controller;

    public ModelPredictionControllerTests()
    {
        _modelPredictionServiceMock = new Mock<IModelPredictionService>();
        _controller = new ModelPredictionController(_modelPredictionServiceMock.Object);
    }

    [Fact]
    public async Task GetLatestPrediction_ExistingPrediction_ReturnsOkWithPrediction()
    {
        // Arrange
        string symbol = "AAPL";
        string modelSource = "Apollo";
        var prediction = new Prediction 
        { 
            Id = 1, 
            Symbol = symbol, 
            ModelSource = modelSource,
            ModelVersion = "1.0",
            Timestamp = DateTime.UtcNow,
            PredictionTarget = DateTime.UtcNow.AddDays(1)
        };

        _modelPredictionServiceMock.Setup(s => s.GetLatestPredictionAsync(symbol, modelSource))
            .ReturnsAsync(prediction);

        // Act
        var result = await _controller.GetLatestPrediction(symbol, modelSource);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedPrediction = Assert.IsType<Prediction>(okResult.Value);
        Assert.Equal(symbol, returnedPrediction.Symbol);
        Assert.Equal(modelSource, returnedPrediction.ModelSource);
    }

    [Fact]
    public async Task GetLatestPrediction_NonExistingPrediction_ReturnsNotFound()
    {
        // Arrange
        string symbol = "AAPL";
        string modelSource = "Apollo";

        _modelPredictionServiceMock.Setup(s => s.GetLatestPredictionAsync(symbol, modelSource))
            .ReturnsAsync((Prediction)null);

        // Act
        var result = await _controller.GetLatestPrediction(symbol, modelSource);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"No prediction found for {symbol} from {modelSource}", 
            notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetLatestPrediction_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        string symbol = "AAPL";
        string modelSource = "Apollo";

        _modelPredictionServiceMock.Setup(s => s.GetLatestPredictionAsync(symbol, modelSource))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetLatestPrediction(symbol, modelSource);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetAllLatestPredictions_ExistingPredictions_ReturnsOkWithPredictions()
    {
        // Arrange
        string symbol = "AAPL";
        var predictions = new Dictionary<string, Prediction>
        {
            { "Apollo", new Prediction { Symbol = symbol, ModelSource = "Apollo", ModelVersion = "1.0", Timestamp = DateTime.UtcNow, PredictionTarget = DateTime.UtcNow.AddDays(1) } },
            { "Ignis", new Prediction { Symbol = symbol, ModelSource = "Ignis", ModelVersion = "1.0", Timestamp = DateTime.UtcNow, PredictionTarget = DateTime.UtcNow.AddDays(1) } }
        };

        _modelPredictionServiceMock.Setup(s => s.GetAllLatestPredictionsAsync(symbol))
            .ReturnsAsync(predictions);

        // Act
        var result = await _controller.GetAllLatestPredictions(symbol);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedPredictions = Assert.IsType<Dictionary<string, Prediction>>(okResult.Value);
        Assert.Equal(2, returnedPredictions.Count);
        Assert.True(returnedPredictions.ContainsKey("Apollo"));
        Assert.True(returnedPredictions.ContainsKey("Ignis"));
    }

    [Fact]
    public async Task GetAllLatestPredictions_NoPredictions_ReturnsNotFound()
    {
        // Arrange
        string symbol = "AAPL";

        _modelPredictionServiceMock.Setup(s => s.GetAllLatestPredictionsAsync(symbol))
            .ReturnsAsync(new Dictionary<string, Prediction>());

        // Act
        var result = await _controller.GetAllLatestPredictions(symbol);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"No predictions found for {symbol}", 
            notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetAllLatestPredictions_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        string symbol = "AAPL";

        _modelPredictionServiceMock.Setup(s => s.GetAllLatestPredictionsAsync(symbol))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetAllLatestPredictions(symbol);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetHistoricalPredictions_ReturnsPredictions()
    {
        // Arrange
        string symbol = "AAPL";
        string modelSource = "Apollo";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        
        var predictions = new List<Prediction>
        {
            new Prediction { Symbol = symbol, ModelSource = modelSource, ModelVersion = "1.0", Timestamp = startDate.AddDays(5), PredictionTarget = startDate.AddDays(6) },
            new Prediction { Symbol = symbol, ModelSource = modelSource, ModelVersion = "1.0", Timestamp = startDate.AddDays(10), PredictionTarget = startDate.AddDays(11) }
        };

        _modelPredictionServiceMock.Setup(s => s.GetHistoricalPredictionsAsync(symbol, modelSource, startDate, endDate))
            .ReturnsAsync(predictions);

        // Act
        var result = await _controller.GetHistoricalPredictions(symbol, modelSource, startDate, endDate);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedPredictions = Assert.IsAssignableFrom<IEnumerable<Prediction>>(okResult.Value);
        Assert.Equal(2, returnedPredictions.Count());
    }

    [Fact]
    public async Task GetHistoricalPredictions_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        string symbol = "AAPL";
        string modelSource = "Apollo";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        _modelPredictionServiceMock.Setup(s => s.GetHistoricalPredictionsAsync(symbol, modelSource, startDate, endDate))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetHistoricalPredictions(symbol, modelSource, startDate, endDate);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task RefreshPredictions_SuccessfulRefresh_ReturnsOkWithPredictions()
    {
        // Arrange
        string symbol = "AAPL";
        var predictions = new Dictionary<string, Prediction>
        {
            { "Apollo", new Prediction { Symbol = symbol, ModelSource = "Apollo", ModelVersion = "1.0", Timestamp = DateTime.UtcNow, PredictionTarget = DateTime.UtcNow.AddDays(1) } },
            { "Ignis", new Prediction { Symbol = symbol, ModelSource = "Ignis", ModelVersion = "1.0", Timestamp = DateTime.UtcNow, PredictionTarget = DateTime.UtcNow.AddDays(1) } }
        };

        _modelPredictionServiceMock.Setup(s => s.RefreshPredictionsAsync(symbol))
            .ReturnsAsync(predictions);

        // Act
        var result = await _controller.RefreshPredictions(symbol);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedPredictions = Assert.IsType<Dictionary<string, Prediction>>(okResult.Value);
        Assert.Equal(2, returnedPredictions.Count);
    }

    [Fact]
    public async Task RefreshPredictions_NoPredictions_ReturnsNotFound()
    {
        // Arrange
        string symbol = "AAPL";

        _modelPredictionServiceMock.Setup(s => s.RefreshPredictionsAsync(symbol))
            .ReturnsAsync(new Dictionary<string, Prediction>());

        // Act
        var result = await _controller.RefreshPredictions(symbol);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"Failed to get predictions for {symbol}", 
            notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task RefreshPredictions_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        string symbol = "AAPL";

        _modelPredictionServiceMock.Setup(s => s.RefreshPredictionsAsync(symbol))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.RefreshPredictions(symbol);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task RefreshPortfolioPredictions_SuccessfulRefresh_ReturnsOkWithPredictions()
    {
        // Arrange
        string portfolioId = "portfolio-123";
        var predictions = new Dictionary<string, Prediction>
        {
            { "AAPL", new Prediction { Symbol = "AAPL", ModelSource = "Gaia", ModelVersion = "1.0", Timestamp = DateTime.UtcNow, PredictionTarget = DateTime.UtcNow.AddDays(1) } },
            { "MSFT", new Prediction { Symbol = "MSFT", ModelSource = "Gaia", ModelVersion = "1.0", Timestamp = DateTime.UtcNow, PredictionTarget = DateTime.UtcNow.AddDays(1) } }
        };

        _modelPredictionServiceMock.Setup(s => s.RefreshPortfolioPredictionsAsync(portfolioId))
            .ReturnsAsync(predictions);

        // Act
        var result = await _controller.RefreshPortfolioPredictions(portfolioId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedPredictions = Assert.IsType<Dictionary<string, Prediction>>(okResult.Value);
        Assert.Equal(2, returnedPredictions.Count);
    }

    [Fact]
    public async Task RefreshPortfolioPredictions_NoPredictions_ReturnsNotFound()
    {
        // Arrange
        string portfolioId = "portfolio-123";

        _modelPredictionServiceMock.Setup(s => s.RefreshPortfolioPredictionsAsync(portfolioId))
            .ReturnsAsync(new Dictionary<string, Prediction>());

        // Act
        var result = await _controller.RefreshPortfolioPredictions(portfolioId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"Failed to get predictions for portfolio {portfolioId}", 
            notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task RefreshPortfolioPredictions_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        string portfolioId = "portfolio-123";

        _modelPredictionServiceMock.Setup(s => s.RefreshPortfolioPredictionsAsync(portfolioId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.RefreshPortfolioPredictions(portfolioId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task StorePrediction_ValidPrediction_ReturnsCreatedWithPrediction()
    {
        // Arrange
        var prediction = new Prediction
        {
            Symbol = "AAPL",
            ModelSource = "Apollo",
            ModelVersion = "1.0",
            Timestamp = DateTime.UtcNow,
            PredictionTarget = DateTime.UtcNow.AddDays(1)
        };

        var storedPrediction = new Prediction
        {
            Id = 1,
            Symbol = "AAPL",
            ModelSource = "Apollo",
            ModelVersion = "1.0",
            Timestamp = DateTime.UtcNow,
            PredictionTarget = DateTime.UtcNow.AddDays(1)
        };

        _modelPredictionServiceMock.Setup(s => s.StorePredictionAsync(prediction))
            .ReturnsAsync(storedPrediction);

        // Act
        var result = await _controller.StorePrediction(prediction);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal("GetLatestPrediction", createdAtActionResult.ActionName);
        Assert.Equal(prediction.Symbol, createdAtActionResult.RouteValues?["symbol"]);
        Assert.Equal(prediction.ModelSource, createdAtActionResult.RouteValues?["modelSource"]);
        
        var returnedPrediction = Assert.IsType<Prediction>(createdAtActionResult.Value);
        Assert.Equal(storedPrediction.Id, returnedPrediction.Id);
    }

    [Fact]
    public async Task StorePrediction_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var prediction = new Prediction
        {
            Symbol = "AAPL",
            ModelSource = "Apollo",
            ModelVersion = "1.0",
            Timestamp = DateTime.UtcNow,
            PredictionTarget = DateTime.UtcNow.AddDays(1)
        };

        _modelPredictionServiceMock.Setup(s => s.StorePredictionAsync(prediction))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.StorePrediction(prediction);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 