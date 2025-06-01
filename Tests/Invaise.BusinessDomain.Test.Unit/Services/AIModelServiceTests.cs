using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using ILogger = Serilog.ILogger;
using System.Linq.Expressions;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class AIModelServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly AIModelService _service;
    private readonly Mock<ILogger> _logger;
    private readonly DbContextOptions<InvaiseDbContext> _options;

    public AIModelServiceTests()
    {
        // Setup in-memory database
        _options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: $"AIModelTestDb_{Guid.NewGuid()}")
            .Options;

        // Create DbContext with in-memory database
        _dbContext = new InvaiseDbContext(_options);
        
        // Create test data
        SeedDatabase();
        
        // Setup mock logger
        _logger = new Mock<ILogger>();
        
        // Create service with real DbContext
        _service = new AIModelService(_dbContext, _logger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        // Add sample data to the in-memory database
        _dbContext.AIModels.AddRange(new List<AIModel>
        {
            new AIModel 
            { 
                Id = 1, 
                Name = "Model 1", 
                Description = "Description 1", 
                ModelStatus = AIModelStatus.Active,
                LastUpdated = DateTime.UtcNow
            },
            new AIModel 
            { 
                Id = 2, 
                Name = "Model 2", 
                Description = "Description 2", 
                ModelStatus = AIModelStatus.Active,
                LastUpdated = DateTime.UtcNow
            },
            new AIModel 
            { 
                Id = 3, 
                Name = "Model 3", 
                Description = "Description 3", 
                ModelStatus = AIModelStatus.Inactive,
                LastUpdated = DateTime.UtcNow
            }
        });
        
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateModelAsync_AddsModelToContext_ReturnsModel()
    {
        // Arrange
        var model = new AIModel
        {
            Name = "Test Model",
            Description = "A test model",
            ModelStatus = AIModelStatus.Active
        };

        // Act
        var result = await _service.CreateModelAsync(model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Model", result.Name);
        
        // Verify model was added to database
        var addedModel = await _dbContext.AIModels.FirstOrDefaultAsync(m => m.Name == "Test Model");
        Assert.NotNull(addedModel);
    }

    [Fact]
    public async Task GetAllModelsAsync_ReturnsAllModels()
    {
        // Act
        var result = await _service.GetAllModelsAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task GetModelByIdAsync_ExistingId_ReturnsModel()
    {
        // Arrange
        long modelId = 1;

        // Act
        var result = await _service.GetModelByIdAsync(modelId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(modelId, result.Id);
        Assert.Equal("Model 1", result.Name);
    }

    [Fact]
    public async Task GetModelByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        long modelId = 999;

        // Act
        var result = await _service.GetModelByIdAsync(modelId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetModelsByStatusAsync_ReturnsFilteredModels()
    {
        // Arrange
        var status = AIModelStatus.Active;
        
        // Act
        var result = await _service.GetModelsByStatusAsync(status);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, m => Assert.Equal(status, m.ModelStatus));
    }

    [Fact]
    public async Task UpdateModelAsync_UpdatesAndSavesChanges_ReturnsTrue()
    {
        // Arrange
        var model = await _dbContext.AIModels.FindAsync(1L);
        Assert.NotNull(model);
        model.Name = "Updated Model";

        // Act
        var result = await _service.UpdateModelAsync(model);

        // Assert
        Assert.True(result);
        
        // Verify model was updated in database
        var updatedModel = await _dbContext.AIModels.FindAsync(1L);
        Assert.NotNull(updatedModel);
        Assert.Equal("Updated Model", updatedModel.Name);
    }

    [Fact]
    public async Task UpdateModelStatusAsync_ExistingModel_UpdatesStatusAndReturnsTrue()
    {
        // Arrange
        long modelId = 1;
        var status = AIModelStatus.Training;
        var originalModel = await _dbContext.AIModels.FindAsync(modelId);
        Assert.NotNull(originalModel);
        var originalLastUpdated = originalModel.LastUpdated;

        // Act
        var result = await _service.UpdateModelStatusAsync(modelId, status);

        // Assert
        Assert.True(result);
        
        // Verify model was updated in database
        var updatedModel = await _dbContext.AIModels.FindAsync(modelId);
        Assert.NotNull(updatedModel);
        Assert.Equal(status, updatedModel.ModelStatus);
        Assert.NotEqual(originalLastUpdated, updatedModel.LastUpdated);
    }

    [Fact]
    public async Task UpdateModelStatusAsync_NonExistingModel_ReturnsFalse()
    {
        // Arrange
        long modelId = 999;
        var status = AIModelStatus.Training;

        // Act
        var result = await _service.UpdateModelStatusAsync(modelId, status);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateModelTrainingDateAsync_ExistingModel_UpdatesDateAndReturnsTrue()
    {
        // Arrange
        long modelId = 1;
        var trainedAt = DateTime.UtcNow;
        var originalModel = await _dbContext.AIModels.FindAsync(modelId);
        Assert.NotNull(originalModel);
        var originalLastUpdated = originalModel.LastUpdated;

        // Act
        var result = await _service.UpdateModelTrainingDateAsync(modelId, trainedAt);

        // Assert
        Assert.True(result);
        
        // Verify model was updated in database
        var updatedModel = await _dbContext.AIModels.FindAsync(modelId);
        Assert.NotNull(updatedModel);
        Assert.Equal(trainedAt, updatedModel.LastTrainedAt);
        Assert.NotEqual(originalLastUpdated, updatedModel.LastUpdated);
    }

    [Fact]
    public async Task UpdateModelTrainingDateAsync_NonExistingModel_ReturnsFalse()
    {
        // Arrange
        long modelId = 999;
        var trainedAt = DateTime.UtcNow;

        // Act
        var result = await _service.UpdateModelTrainingDateAsync(modelId, trainedAt);

        // Assert
        Assert.False(result);
    }
}

// Helper classes for async enumeration support
internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _enumerator;

    public TestAsyncEnumerator(IEnumerator<T> enumerator)
    {
        _enumerator = enumerator;
    }

    public T Current => _enumerator.Current;

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_enumerator.MoveNext());

    public ValueTask DisposeAsync()
    {
        _enumerator.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>
{
    public TestDbAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestDbAsyncEnumerable(Expression expression) : base(expression) { }
    
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
} 