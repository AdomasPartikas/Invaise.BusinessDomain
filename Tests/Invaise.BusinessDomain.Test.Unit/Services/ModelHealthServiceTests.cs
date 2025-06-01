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

public class ModelHealthServiceTests : TestBase
{
    private readonly ModelHealthService _service;
    private readonly Mock<IAIModelService> _aiModelServiceMock;
    private readonly Mock<IApolloService> _apolloServiceMock;
    private readonly Mock<IIgnisService> _ignisServiceMock;
    private readonly Mock<IGaiaService> _gaiaServiceMock;
    private readonly Mock<ILogger> _serilogLoggerMock;

    public ModelHealthServiceTests()
    {
        // Setup mocks
        _aiModelServiceMock = new Mock<IAIModelService>();
        _apolloServiceMock = new Mock<IApolloService>();
        _ignisServiceMock = new Mock<IIgnisService>();
        _gaiaServiceMock = new Mock<IGaiaService>();
        _serilogLoggerMock = new Mock<ILogger>();
        
        // Create service with mocks
        _service = new ModelHealthService(
            _aiModelServiceMock.Object,
            _apolloServiceMock.Object,
            _ignisServiceMock.Object,
            _gaiaServiceMock.Object,
            _serilogLoggerMock.Object
        );
    }

    [Fact]
    public async Task CheckModelHealthAsync_WithValidApolloModel_ReturnsHealthStatus()
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
            
        _apolloServiceMock.Setup(service => service.CheckHealthAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _service.CheckModelHealthAsync(1);

        // Assert
        Assert.True(result);
        _apolloServiceMock.Verify(service => service.CheckHealthAsync(), Times.Once);
    }
    
    [Fact]
    public async Task CheckModelHealthAsync_WithValidIgnisModel_ReturnsHealthStatus()
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
            
        _ignisServiceMock.Setup(service => service.CheckHealthAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _service.CheckModelHealthAsync(2);

        // Assert
        Assert.True(result);
        _ignisServiceMock.Verify(service => service.CheckHealthAsync(), Times.Once);
    }
    
    [Fact]
    public async Task CheckModelHealthAsync_WithValidGaiaModel_ReturnsHealthStatus()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 3,
            Name = "Gaia",
            ModelStatus = AIModelStatus.Active
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(3))
            .ReturnsAsync(model);
            
        _gaiaServiceMock.Setup(service => service.CheckHealthAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _service.CheckModelHealthAsync(3);

        // Assert
        Assert.True(result);
        _gaiaServiceMock.Verify(service => service.CheckHealthAsync(), Times.Once);
    }
    
    [Fact]
    public async Task CheckModelHealthAsync_WithUnknownModel_ReturnsFalse()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 4,
            Name = "UnknownModel",
            ModelStatus = AIModelStatus.Active
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(4))
            .ReturnsAsync(model);

        // Act
        var result = await _service.CheckModelHealthAsync(4);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task CheckModelHealthAsync_WithNonExistentModel_ReturnsFalse()
    {
        // Arrange
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(99))
            .ReturnsAsync((AIModel?)null);

        // Act
        var result = await _service.CheckModelHealthAsync(99);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task CheckModelHealthAsync_WhenServiceThrowsException_ReturnsFalse()
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
            
        _apolloServiceMock.Setup(service => service.CheckHealthAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.CheckModelHealthAsync(1);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task UpdateModelHealthStatusAsync_WithHealthyModel_UpdatesStatusToActive()
    {
        // Arrange
        var model = new AIModel
        {
            Id = 1,
            Name = "Apollo",
            ModelStatus = AIModelStatus.Inactive
        };
        
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(1))
            .ReturnsAsync(model);
            
        _aiModelServiceMock.Setup(service => service.UpdateModelStatusAsync(1, AIModelStatus.Active))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateModelHealthStatusAsync(1, true);

        // Assert
        Assert.True(result);
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(1, AIModelStatus.Active), Times.Once);
    }
    
    [Fact]
    public async Task UpdateModelHealthStatusAsync_WithUnhealthyModel_UpdatesStatusToInactive()
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
            
        _aiModelServiceMock.Setup(service => service.UpdateModelStatusAsync(1, AIModelStatus.Inactive))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateModelHealthStatusAsync(1, false);

        // Assert
        Assert.True(result);
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(1, AIModelStatus.Inactive), Times.Once);
    }
    
    [Fact]
    public async Task UpdateModelHealthStatusAsync_WithSameStatus_DoesNotUpdateStatus()
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

        // Act
        var result = await _service.UpdateModelHealthStatusAsync(1, true);

        // Assert
        Assert.True(result);
        _aiModelServiceMock.Verify(service => service.UpdateModelStatusAsync(It.IsAny<long>(), It.IsAny<AIModelStatus>()), Times.Never);
    }
    
    [Fact]
    public async Task UpdateModelHealthStatusAsync_WithNonExistentModel_ReturnsFalse()
    {
        // Arrange
        _aiModelServiceMock.Setup(service => service.GetModelByIdAsync(99))
            .ReturnsAsync((AIModel?)null);

        // Act
        var result = await _service.UpdateModelHealthStatusAsync(99, true);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task CheckAllModelsHealthAsync_WithNoModels_ReturnsEmptyDictionary()
    {
        // Arrange
        _aiModelServiceMock.Setup(service => service.GetAllModelsAsync())
            .ReturnsAsync([]);

        // Act
        var result = await _service.CheckAllModelsHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
} 