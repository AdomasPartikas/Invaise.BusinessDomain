using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class AIModelControllerTests : TestBase
{
    private readonly Mock<IAIModelService> _aiModelServiceMock;
    private readonly AIModelController _controller;

    public AIModelControllerTests()
    {
        _aiModelServiceMock = new Mock<IAIModelService>();
        _controller = new AIModelController(_aiModelServiceMock.Object);
    }

    [Fact]
    public async Task CreateModel_ValidModel_ReturnsCreatedAtAction()
    {
        // Arrange
        var model = new AIModel
        {
            Name = "Test Model",
            Description = "A test model",
            ModelStatus = AIModelStatus.Active
        };

        var createdModel = new AIModel
        {
            Id = 1,
            Name = "Test Model",
            Description = "A test model",
            ModelStatus = AIModelStatus.Active,
            CreatedAt = DateTime.Now
        };

        _aiModelServiceMock.Setup(s => s.CreateModelAsync(It.IsAny<AIModel>()))
            .ReturnsAsync(createdModel);

        // Act
        var result = await _controller.CreateModel(model);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal("GetModelById", createdAtActionResult.ActionName);
        Assert.Equal(createdModel.Id, createdAtActionResult.RouteValues?["id"]);
        
        var returnedModel = Assert.IsType<AIModel>(createdAtActionResult.Value);
        Assert.Equal(createdModel.Id, returnedModel.Id);
        Assert.Equal(createdModel.Name, returnedModel.Name);
    }

    [Fact]
    public async Task CreateModel_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var model = new AIModel
        {
            Name = "Test Model",
            Description = "A test model"
        };

        _aiModelServiceMock.Setup(s => s.CreateModelAsync(It.IsAny<AIModel>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.CreateModel(model);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("Error creating AI model", statusCodeResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetAllModels_ReturnsOkWithModels()
    {
        // Arrange
        var models = new List<AIModel>
        {
            new AIModel { Id = 1, Name = "Model 1" },
            new AIModel { Id = 2, Name = "Model 2" }
        };

        _aiModelServiceMock.Setup(s => s.GetAllModelsAsync())
            .ReturnsAsync(models);

        // Act
        var result = await _controller.GetAllModels();

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedModels = Assert.IsAssignableFrom<IEnumerable<AIModel>>(okResult.Value);
        Assert.Equal(2, returnedModels.Count());
    }

    [Fact]
    public async Task GetAllModels_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _aiModelServiceMock.Setup(s => s.GetAllModelsAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetAllModels();

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetModelById_ExistingId_ReturnsOkWithModel()
    {
        // Arrange
        long modelId = 1;
        var model = new AIModel
        {
            Id = modelId,
            Name = "Test Model",
            Description = "A test model"
        };

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync(model);

        // Act
        var result = await _controller.GetModelById(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedModel = Assert.IsType<AIModel>(okResult.Value);
        Assert.Equal(modelId, returnedModel.Id);
    }

    [Fact]
    public async Task GetModelById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        long modelId = 999;

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ReturnsAsync((AIModel)null);

        // Act
        var result = await _controller.GetModelById(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"AI model with ID {modelId} not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetModelById_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        long modelId = 1;

        _aiModelServiceMock.Setup(s => s.GetModelByIdAsync(modelId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetModelById(modelId);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetModelsByStatus_ReturnsOkWithFilteredModels()
    {
        // Arrange
        var status = AIModelStatus.Active;
        var models = new List<AIModel>
        {
            new AIModel { Id = 1, Name = "Model 1", ModelStatus = status },
            new AIModel { Id = 2, Name = "Model 2", ModelStatus = status }
        };

        _aiModelServiceMock.Setup(s => s.GetModelsByStatusAsync(status))
            .ReturnsAsync(models);

        // Act
        var result = await _controller.GetModelsByStatus(status);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedModels = Assert.IsAssignableFrom<IEnumerable<AIModel>>(okResult.Value);
        Assert.Equal(2, returnedModels.Count());
        Assert.All(returnedModels, m => Assert.Equal(status, m.ModelStatus));
    }

    [Fact]
    public async Task GetModelsByStatus_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var status = AIModelStatus.Active;

        _aiModelServiceMock.Setup(s => s.GetModelsByStatusAsync(status))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetModelsByStatus(status);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UpdateModel_ExistingModel_ReturnsOkWithSuccess()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Updated Model",
            Description = "An updated test model"
        };

        _aiModelServiceMock.Setup(s => s.UpdateModelAsync(It.IsAny<AIModel>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateModel(model);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var success = Assert.IsType<bool>(okResult.Value);
        Assert.True(success);
    }

    [Fact]
    public async Task UpdateModel_NonExistingModel_ReturnsNotFound()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 999,
            Name = "Non-existing Model"
        };

        _aiModelServiceMock.Setup(s => s.UpdateModelAsync(It.IsAny<AIModel>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateModel(model);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"AI model with ID {model.Id} not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task UpdateModel_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Test Model"
        };

        _aiModelServiceMock.Setup(s => s.UpdateModelAsync(It.IsAny<AIModel>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.UpdateModel(model);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UpdateModelStatus_ExistingId_ReturnsOkWithSuccess()
    {
        // Arrange
        long modelId = 1;
        var status = AIModelStatus.Training;

        _aiModelServiceMock.Setup(s => s.UpdateModelStatusAsync(modelId, status))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateModelStatus(modelId, status);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var success = Assert.IsType<bool>(okResult.Value);
        Assert.True(success);
    }

    [Fact]
    public async Task UpdateModelStatus_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        long modelId = 999;
        var status = AIModelStatus.Training;

        _aiModelServiceMock.Setup(s => s.UpdateModelStatusAsync(modelId, status))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateModelStatus(modelId, status);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"AI model with ID {modelId} not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task UpdateModelStatus_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        long modelId = 1;
        var status = AIModelStatus.Training;

        _aiModelServiceMock.Setup(s => s.UpdateModelStatusAsync(modelId, status))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.UpdateModelStatus(modelId, status);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UpdateModelTrainingDate_ExistingId_ReturnsOkWithSuccess()
    {
        // Arrange
        long modelId = 1;
        var trainedAt = DateTime.UtcNow;

        _aiModelServiceMock.Setup(s => s.UpdateModelTrainingDateAsync(modelId, It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateModelTrainingDate(modelId, trainedAt);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var success = Assert.IsType<bool>(okResult.Value);
        Assert.True(success);
    }

    [Fact]
    public async Task UpdateModelTrainingDate_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        long modelId = 999;
        var trainedAt = DateTime.UtcNow;

        _aiModelServiceMock.Setup(s => s.UpdateModelTrainingDateAsync(modelId, It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateModelTrainingDate(modelId, trainedAt);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains($"AI model with ID {modelId} not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task UpdateModelTrainingDate_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        long modelId = 1;
        var trainedAt = DateTime.UtcNow;

        _aiModelServiceMock.Setup(s => s.UpdateModelTrainingDateAsync(modelId, It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.UpdateModelTrainingDate(modelId, trainedAt);

        // Assert
        var actionResult = result.Result;
        Assert.NotNull(actionResult);
        
        var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 