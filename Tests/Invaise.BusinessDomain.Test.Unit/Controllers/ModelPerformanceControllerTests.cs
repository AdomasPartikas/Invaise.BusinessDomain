using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class ModelPerformanceControllerTests : TestBase
{
    private readonly Mock<IModelPerformanceService> _modelPerformanceServiceMock;
    private readonly Mock<IAIModelService> _aiModelServiceMock;
    private readonly ModelPerformanceController _controller;

    public ModelPerformanceControllerTests()
    {
        _modelPerformanceServiceMock = new Mock<IModelPerformanceService>();
        _aiModelServiceMock = new Mock<IAIModelService>();
        _controller = new ModelPerformanceController(_modelPerformanceServiceMock.Object, _aiModelServiceMock.Object);
    }

    [Fact]
    public async Task GetTrainingStatus_ReturnsOkWithResults()
    {
        // Arrange
        var trainingStatus = new Dictionary<long, bool>
        {
            { 1, true },
            { 2, false }
        };

        _modelPerformanceServiceMock.Setup(s => s.CheckTrainingModelsStatusAsync())
            .ReturnsAsync(trainingStatus);

        // Act
        var result = await _controller.GetTrainingStatus();

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedStatus = Assert.IsType<Dictionary<long, bool>>(okResult.Value);
        Assert.Equal(2, returnedStatus.Count);
        Assert.True(returnedStatus[1]);
        Assert.False(returnedStatus[2]);
    }

    [Fact]
    public async Task GetTrainingStatus_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _modelPerformanceServiceMock.Setup(s => s.CheckTrainingModelsStatusAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetTrainingStatus();

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("Error checking training status", statusCodeResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CheckIfNeedsRetraining_ReturnsOkWithResult()
    {
        // Arrange
        long modelId = 1;
        bool needsRetraining = true;

        _modelPerformanceServiceMock.Setup(s => s.CheckIfModelNeedsRetrainingAsync(modelId))
            .ReturnsAsync(needsRetraining);

        // Act
        var result = await _controller.CheckIfNeedsRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedResult = Assert.IsType<bool>(okResult.Value);
        Assert.True(returnedResult);
    }

    [Fact]
    public async Task CheckIfNeedsRetraining_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        long modelId = 1;

        _modelPerformanceServiceMock.Setup(s => s.CheckIfModelNeedsRetrainingAsync(modelId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.CheckIfNeedsRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("Error checking if model needs retraining", statusCodeResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task InitiateRetraining_ModelExists_ReturnsOkWithResult()
    {
        // Arrange
        long modelId = 1;
        var model = new AIModel
        {
            Id = modelId,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Active
        };
        
        bool initiatedSuccessfully = true;

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync(model);
            
        _modelPerformanceServiceMock.Setup(s => s.InitiateModelRetrainingAsync(modelId))
            .ReturnsAsync(initiatedSuccessfully);

        // Act
        var result = await _controller.InitiateRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedResult = Assert.IsType<bool>(okResult.Value);
        Assert.True(returnedResult);
    }

    [Fact]
    public async Task InitiateRetraining_ModelNotFound_ReturnsNotFound()
    {
        // Arrange
        long modelId = 999;

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync((AIModel)null);

        // Act
        var result = await _controller.InitiateRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"Model with ID {modelId} not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task InitiateRetraining_UnsupportedModel_ReturnsBadRequest()
    {
        // Arrange
        long modelId = 1;
        var model = new AIModel
        {
            Id = modelId,
            Name = "Unsupported",
            ModelStatus = AIModelStatus.Active
        };

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync(model);

        // Act
        var result = await _controller.InitiateRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Contains($"Model {model.Name} does not support retraining", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task InitiateRetraining_ModelAlreadyTraining_ReturnsBadRequest()
    {
        // Arrange
        long modelId = 1;
        var model = new AIModel
        {
            Id = modelId,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Training
        };

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync(model);

        // Act
        var result = await _controller.InitiateRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Contains($"Model {model.Name} is already training", badRequestResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task InitiateRetraining_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        long modelId = 1;
        var model = new AIModel
        {
            Id = modelId,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Active
        };

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync(model);
            
        _modelPerformanceServiceMock.Setup(s => s.InitiateModelRetrainingAsync(modelId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.InitiateRetraining(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("Error initiating model retraining", statusCodeResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CheckAndRetrainAllModels_ReturnsOkWithResults()
    {
        // Arrange
        var retrainingResults = new Dictionary<long, bool>
        {
            { 1, true },
            { 2, false }
        };

        _modelPerformanceServiceMock.Setup(s => s.CheckAndInitiateRetrainingForAllModelsAsync())
            .ReturnsAsync(retrainingResults);

        // Act
        var result = await _controller.CheckAndRetrainAllModels();

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedResults = Assert.IsType<Dictionary<long, bool>>(okResult.Value);
        Assert.Equal(2, returnedResults.Count);
        Assert.True(returnedResults[1]);
        Assert.False(returnedResults[2]);
    }

    [Fact]
    public async Task CheckAndRetrainAllModels_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _modelPerformanceServiceMock.Setup(s => s.CheckAndInitiateRetrainingForAllModelsAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.CheckAndRetrainAllModels();

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("Error checking and retraining models", statusCodeResult.Value?.ToString() ?? string.Empty);
    }
} 