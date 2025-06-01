using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class ModelPerformanceServiceTests : TestBase
{
    private readonly ModelPerformanceService _service;
    private readonly Mock<IAIModelService> _aiModelServiceMock;
    private readonly Mock<IApolloService> _apolloServiceMock;
    private readonly Mock<IIgnisService> _ignisServiceMock;
    private readonly Mock<ILogger> _serilogLoggerMock;
    
    public ModelPerformanceServiceTests()
    {
        // Setup mocks
        _aiModelServiceMock = new Mock<IAIModelService>();
        _apolloServiceMock = new Mock<IApolloService>();
        _ignisServiceMock = new Mock<IIgnisService>();
        _serilogLoggerMock = new Mock<ILogger>();
        
        // Create service with mocks
        _service = new ModelPerformanceService(
            _aiModelServiceMock.Object,
            _apolloServiceMock.Object,
            _ignisServiceMock.Object,
            _serilogLoggerMock.Object
        );
    }
    
    [Fact]
    public async Task CheckIfModelNeedsRetrainingAsync_WithNonExistentModel_ReturnsFalse()
    {
        // Arrange
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(99))
            .ReturnsAsync((AIModel)null);
            
        // Act
        var result = await _service.CheckIfModelNeedsRetrainingAsync(99);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task CheckIfModelNeedsRetrainingAsync_WithModelInTraining_ReturnsFalse()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Training
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        // Act
        var result = await _service.CheckIfModelNeedsRetrainingAsync(1);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task CheckIfModelNeedsRetrainingAsync_WithNeverTrainedModel_ReturnsTrue()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Active,
            LastTrainedAt = null
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        // Act
        var result = await _service.CheckIfModelNeedsRetrainingAsync(1);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task CheckIfModelNeedsRetrainingAsync_WithRecentlyTrainedModel_ReturnsFalse()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Active,
            LastTrainedAt = DateTime.UtcNow.AddHours(-6) // Less than 1 day ago
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        // Act
        var result = await _service.CheckIfModelNeedsRetrainingAsync(1);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task CheckIfModelNeedsRetrainingAsync_WithOldTrainingDate_ReturnsTrue()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Active,
            LastTrainedAt = DateTime.UtcNow.AddDays(-2) // More than 1 day ago
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        // Act
        var result = await _service.CheckIfModelNeedsRetrainingAsync(1);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task InitiateModelRetrainingAsync_WithNonExistentModel_ReturnsFalse()
    {
        // Arrange
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(99))
            .ReturnsAsync((AIModel)null);
            
        // Act
        var result = await _service.InitiateModelRetrainingAsync(99);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task InitiateModelRetrainingAsync_WithUnsupportedModel_ReturnsFalse()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 3,
            Name = "Gaia", // Only Apollo and Ignis support retraining
            ModelStatus = AIModelStatus.Active
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(3))
            .ReturnsAsync(model);
            
        // Act
        var result = await _service.InitiateModelRetrainingAsync(3);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task InitiateModelRetrainingAsync_WithModelAlreadyTraining_ReturnsFalse()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Training
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        // Act
        var result = await _service.InitiateModelRetrainingAsync(1);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task InitiateModelRetrainingAsync_WithApolloModel_RequestsRetraining()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Active
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        _apolloServiceMock.Setup(service => service.RequestRetrainingAsync())
            .ReturnsAsync(true);
            
        _aiModelServiceMock.Setup(service => service.UpdateModelStatusAsync(1, AIModelStatus.Training))
            .ReturnsAsync(true);
            
        // Act
        var result = await _service.InitiateModelRetrainingAsync(1);
        
        // Assert
        Assert.True(result);
        _apolloServiceMock.Verify(service => service.RequestRetrainingAsync(), Times.Once);
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(1, AIModelStatus.Training), Times.Once);
    }
    
    [Fact]
    public async Task InitiateModelRetrainingAsync_WithIgnisModel_RequestsRetraining()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 2,
            Name = "Ignis",
            ModelStatus = AIModelStatus.Active
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(2))
            .ReturnsAsync(model);
            
        _ignisServiceMock.Setup(service => service.RequestRetrainingAsync())
            .ReturnsAsync(true);
            
        _aiModelServiceMock.Setup(service => service.UpdateModelStatusAsync(2, AIModelStatus.Training))
            .ReturnsAsync(true);
            
        // Act
        var result = await _service.InitiateModelRetrainingAsync(2);
        
        // Assert
        Assert.True(result);
        _ignisServiceMock.Verify(service => service.RequestRetrainingAsync(), Times.Once);
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(2, AIModelStatus.Training), Times.Once);
    }
    
    [Fact]
    public async Task CheckTrainingModelsStatusAsync_WithNoTrainingModels_ReturnsEmptyDictionary()
    {
        // Arrange
        _aiModelServiceMock.Setup(service => service.GetModelsByStatusAsync(AIModelStatus.Training))
            .ReturnsAsync(new List<AIModel>());
            
        // Act
        var result = await _service.CheckTrainingModelsStatusAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task CheckTrainingModelsStatusAsync_WithTrainingModels_ChecksStatusForEachModel()
    {
        // Arrange
        var models = new List<AIModel>
        {
            new AIModel { Id = 1, Name = "Apollo", ModelStatus = AIModelStatus.Training },
            new AIModel { Id = 2, Name = "Ignis", ModelStatus = AIModelStatus.Training }
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelsByStatusAsync(AIModelStatus.Training))
            .ReturnsAsync(models);
            
        _apolloServiceMock.Setup(service => service.IsTrainingAsync())
            .ReturnsAsync(false); // Training completed
            
        _ignisServiceMock.Setup(service => service.IsTrainingAsync())
            .ReturnsAsync(true); // Still training
            
        _aiModelServiceMock.Setup(service => service.UpdateModelStatusAsync(It.IsAny<long>(), It.IsAny<AIModelStatus>()))
            .ReturnsAsync(true);
            
        _aiModelServiceMock.Setup(service => service.UpdateModelTrainingDateAsync(It.IsAny<long>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true);
            
        // Act
        var result = await _service.CheckTrainingModelsStatusAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.False(result[1]); // Apollo finished training
        Assert.True(result[2]); // Ignis still training
        
        // Verify status updates - only Apollo should be updated
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(1, AIModelStatus.Active), Times.Once);
        _aiModelServiceMock.Verify(service => service.UpdateModelTrainingDateAsync(1, It.IsAny<DateTime>()), Times.Once);
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(2, It.IsAny<AIModelStatus>()), Times.Never);
    }
} 