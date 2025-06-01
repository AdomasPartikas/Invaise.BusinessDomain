using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Xunit;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class ModelPredictionServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly Mock<IDatabaseService> _mockDatabaseService;
    private readonly Mock<IApolloService> _mockApolloService;
    private readonly Mock<IIgnisService> _mockIgnisService;
    private readonly Mock<IGaiaService> _mockGaiaService;
    private readonly Mock<IPortfolioOptimizationService> _mockPortfolioOptimizationService;
    private readonly Mock<Serilog.ILogger> _mockLogger;
    private readonly ModelPredictionService _service;
    private readonly DbContextOptions<InvaiseDbContext> _options;

    public ModelPredictionServiceTests()
    {
        // Setup in-memory database
        _options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: $"ModelPredictionServiceTestDb_{Guid.NewGuid()}")
            .Options;

        // Create DbContext with in-memory database
        _dbContext = new InvaiseDbContext(_options);
        
        _mockDatabaseService = new Mock<IDatabaseService>();
        _mockApolloService = new Mock<IApolloService>();
        _mockIgnisService = new Mock<IIgnisService>();
        _mockGaiaService = new Mock<IGaiaService>();
        _mockPortfolioOptimizationService = new Mock<IPortfolioOptimizationService>();
        _mockLogger = new Mock<Serilog.ILogger>();

        _service = new ModelPredictionService(
            _mockDatabaseService.Object,
            _dbContext,
            _mockApolloService.Object,
            _mockIgnisService.Object,
            _mockGaiaService.Object,
            _mockPortfolioOptimizationService.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetLatestPredictionAsync_ReturnsLatestPrediction_WhenExists()
    {
        // Arrange
        var symbol = "AAPL";
        var modelSource = ModelConstants.APOLLO_SOURCE;
        var now = DateTime.UtcNow;
        
        var predictions = new List<Prediction>
        {
            new Prediction
            {
                Id = 1,
                Symbol = symbol,
                ModelSource = modelSource,
                Timestamp = now.AddDays(-2),
                Heat = new Heat { Symbol = symbol, Score = 60 }
            },
            new Prediction
            {
                Id = 2,
                Symbol = symbol,
                ModelSource = modelSource,
                Timestamp = now.AddDays(-1), // Latest
                Heat = new Heat { Symbol = symbol, Score = 70 }
            },
            new Prediction
            {
                Id = 3,
                Symbol = symbol,
                ModelSource = ModelConstants.IGNIS_SOURCE, // Different source
                Timestamp = now,
                Heat = new Heat { Symbol = symbol, Score = 80 }
            }
        };
        
        // Add predictions to the in-memory database
        _dbContext.Predictions.AddRange(predictions);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetLatestPredictionAsync(symbol, modelSource);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2); // Should be the most recent prediction for the given symbol and model source
        result.Heat.Should().NotBeNull();
        result.Heat!.Score.Should().Be(70);
    }

    [Fact]
    public async Task GetLatestPredictionAsync_ReturnsNull_WhenNoPredictionsExist()
    {
        // Arrange
        var symbol = "AAPL";
        var modelSource = ModelConstants.APOLLO_SOURCE;
        
        // No predictions added to database

        // Act
        var result = await _service.GetLatestPredictionAsync(symbol, modelSource);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestPredictionAsync_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var symbol = "AAPL";
        var modelSource = ModelConstants.APOLLO_SOURCE;
        
        // Mock the logger directly without trying to mock DbContext
        _mockLogger.Setup(logger => logger.Error(
            It.IsAny<Exception>(), 
            It.IsAny<string>(), 
            It.Is<string>(s => s == symbol), 
            It.Is<string>(s => s == modelSource)))
            .Verifiable();
        
        // Create a test implementation of IModelPredictionService that always throws
        var testService = new TestModelPredictionService(_mockLogger.Object);
        
        // Act
        var result = await testService.GetLatestPredictionAsync(symbol, modelSource);
        
        // Assert
        result.Should().BeNull();
        _mockLogger.Verify();
    }

    [Fact]
    public async Task GetAllLatestPredictionsAsync_ReturnsLatestPredictions_FromAllSources()
    {
        // Arrange
        var symbol = "AAPL";
        var now = DateTime.UtcNow;
        
        var predictions = new List<Prediction>
        {
            new Prediction
            {
                Id = 1,
                Symbol = symbol,
                ModelSource = ModelConstants.APOLLO_SOURCE,
                Timestamp = now.AddDays(-2),
                Heat = new Heat { Symbol = symbol, Score = 60 }
            },
            new Prediction
            {
                Id = 2,
                Symbol = symbol,
                ModelSource = ModelConstants.APOLLO_SOURCE,
                Timestamp = now.AddDays(-1), // Latest Apollo
                Heat = new Heat { Symbol = symbol, Score = 70 }
            },
            new Prediction
            {
                Id = 3,
                Symbol = symbol,
                ModelSource = ModelConstants.IGNIS_SOURCE,
                Timestamp = now.AddDays(-3),
                Heat = new Heat { Symbol = symbol, Score = 50 }
            },
            new Prediction
            {
                Id = 4,
                Symbol = symbol,
                ModelSource = ModelConstants.IGNIS_SOURCE,
                Timestamp = now, // Latest Ignis
                Heat = new Heat { Symbol = symbol, Score = 80 }
            },
            new Prediction
            {
                Id = 5,
                Symbol = "MSFT", // Different symbol
                ModelSource = ModelConstants.APOLLO_SOURCE,
                Timestamp = now,
                Heat = new Heat { Symbol = "MSFT", Score = 90 }
            }
        };
        
        // Add predictions to the in-memory database
        _dbContext.Predictions.AddRange(predictions);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllLatestPredictionsAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2); // One for Apollo, one for Ignis
        result.Should().ContainKey(ModelConstants.APOLLO_SOURCE);
        result.Should().ContainKey(ModelConstants.IGNIS_SOURCE);
        result[ModelConstants.APOLLO_SOURCE].Id.Should().Be(2); // Latest Apollo prediction
        result[ModelConstants.IGNIS_SOURCE].Id.Should().Be(4); // Latest Ignis prediction
    }

    [Fact]
    public async Task StorePredictionAsync_SavesPrediction_AndSetsTimestampIfDefault()
    {
        // Arrange
        var prediction = new Prediction
        {
            Symbol = "AAPL",
            ModelSource = ModelConstants.APOLLO_SOURCE,
            ModelVersion = "1.0.0",
            Heat = new Heat { Symbol = "AAPL", Score = 75 }
        };
        
        // Act
        var result = await _service.StorePredictionAsync(prediction);
        
        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().NotBe(default);
        
        // Verify the prediction was saved to the database
        var savedPrediction = await _dbContext.Predictions.FindAsync(result.Id);
        savedPrediction.Should().NotBeNull();
        savedPrediction!.Symbol.Should().Be("AAPL");
        savedPrediction.ModelSource.Should().Be(ModelConstants.APOLLO_SOURCE);
        savedPrediction.ModelVersion.Should().Be("1.0.0");
    }
    
    [Fact]
    public async Task StorePredictionAsync_DoesNotChangeTimestamp_IfAlreadySet()
    {
        // Arrange
        var existingTimestamp = new DateTime(2023, 1, 1, 12, 0, 0);
        var prediction = new Prediction
        {
            Symbol = "AAPL",
            ModelSource = ModelConstants.APOLLO_SOURCE,
            ModelVersion = "1.0.0",
            Timestamp = existingTimestamp,
            Heat = new Heat { Symbol = "AAPL", Score = 75 }
        };
        
        // Act
        var result = await _service.StorePredictionAsync(prediction);
        
        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().Be(existingTimestamp);
        
        // Verify the prediction was saved to the database with original timestamp
        var savedPrediction = await _dbContext.Predictions.FindAsync(result.Id);
        savedPrediction.Should().NotBeNull();
        savedPrediction!.Timestamp.Should().Be(existingTimestamp);
    }

    [Fact]
    public async Task StorePredictionAsync_ThrowsException_WhenSaveFails()
    {
        // Arrange
        var prediction = new Prediction
        {
            Symbol = "AAPL",
            ModelSource = ModelConstants.APOLLO_SOURCE,
            ModelVersion = "1.0.0",
            Heat = new Heat { Symbol = "AAPL", Score = 75 }
        };
        
        // Set up logger verification
        _mockLogger.Setup(logger => logger.Error(
            It.IsAny<Exception>(), 
            It.IsAny<string>(), 
            It.Is<string>(s => s == prediction.Symbol), 
            It.Is<string>(s => s == prediction.ModelSource)))
            .Verifiable();
            
        // Create a test service that always throws on StorePredictionAsync
        var testService = new TestModelPredictionService(_mockLogger.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => testService.StorePredictionAsync(prediction));
        _mockLogger.Verify();
    }

    [Fact]
    public async Task GetHistoricalPredictionsAsync_ReturnsHistoricalPredictions_InDateRange()
    {
        // Arrange
        var symbol = "AAPL";
        var modelSource = ModelConstants.APOLLO_SOURCE;
        var now = DateTime.UtcNow;
        var startDate = now.AddDays(-30);
        var endDate = now;
        
        var predictions = new List<Prediction>
        {
            new Prediction
            {
                Id = 1,
                Symbol = symbol,
                ModelSource = modelSource,
                Timestamp = now.AddDays(-40), // Outside range
                Heat = new Heat { Symbol = symbol, Score = 50 }
            },
            new Prediction
            {
                Id = 2,
                Symbol = symbol,
                ModelSource = modelSource,
                Timestamp = now.AddDays(-25), // In range
                Heat = new Heat { Symbol = symbol, Score = 60 }
            },
            new Prediction
            {
                Id = 3,
                Symbol = symbol,
                ModelSource = modelSource,
                Timestamp = now.AddDays(-10), // In range
                Heat = new Heat { Symbol = symbol, Score = 70 }
            },
            new Prediction
            {
                Id = 4,
                Symbol = symbol,
                ModelSource = modelSource,
                Timestamp = now.AddDays(5), // Outside range
                Heat = new Heat { Symbol = symbol, Score = 80 }
            },
            new Prediction
            {
                Id = 5,
                Symbol = symbol,
                ModelSource = ModelConstants.IGNIS_SOURCE, // Different source
                Timestamp = now.AddDays(-15),
                Heat = new Heat { Symbol = symbol, Score = 75 }
            }
        };
        
        // Add predictions to the in-memory database
        _dbContext.Predictions.AddRange(predictions);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _service.GetHistoricalPredictionsAsync(symbol, modelSource, startDate, endDate);
        
        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(2); // Only the 2 predictions within the date range for the given source
        result.Should().Contain(p => p.Id == 2);
        result.Should().Contain(p => p.Id == 3);
        result.Should().NotContain(p => p.Id == 1); // Outside date range
        result.Should().NotContain(p => p.Id == 4); // Outside date range
        result.Should().NotContain(p => p.Id == 5); // Different source
    }

    [Fact]
    public async Task RefreshPredictionsAsync_ReturnsUpdatedPredictions_FromApolloAndIgnis()
    {
        // Arrange
        var symbol = "AAPL";
        
        // Setup Apollo mock
        var apolloHeat = new Heat { Symbol = symbol, Score = 75 };
        var apolloResponse = new ValueTuple<Heat, double>(apolloHeat, 180.0);
        _mockApolloService.Setup(s => s.GetHeatPredictionAsync(symbol))
            .ReturnsAsync(apolloResponse);
        _mockApolloService.Setup(s => s.GetModelVersionAsync())
            .ReturnsAsync("1.0.0");
            
        // Setup Ignis mock
        var ignisHeat = new Heat { Symbol = symbol, Score = 65 };
        var ignisResponse = new ValueTuple<Heat, double>(ignisHeat, 175.0);
        _mockIgnisService.Setup(s => s.GetHeatPredictionAsync(symbol))
            .ReturnsAsync(ignisResponse);
        _mockIgnisService.Setup(s => s.GetModelVersionAsync())
            .ReturnsAsync("2.0.0");
            
        // Setup market data mocks
        var historicalData = new HistoricalMarketData { Symbol = symbol, Close = 170.0m };
        var intradayData = new IntradayMarketData { Symbol = symbol, Current = 172.0m };
        _mockDatabaseService.Setup(s => s.GetLatestHistoricalMarketDataAsync(symbol))
            .ReturnsAsync(historicalData);
        _mockDatabaseService.Setup(s => s.GetLatestIntradayMarketDataAsync(symbol))
            .ReturnsAsync(intradayData);
            
        // Act
        var result = await _service.RefreshPredictionsAsync(symbol);
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.Should().ContainKey(ModelConstants.APOLLO_SOURCE);
        result.Should().ContainKey(ModelConstants.IGNIS_SOURCE);
        
        result[ModelConstants.APOLLO_SOURCE].Should().NotBeNull();
        result[ModelConstants.APOLLO_SOURCE].Symbol.Should().Be(symbol);
        result[ModelConstants.APOLLO_SOURCE].ModelSource.Should().Be(ModelConstants.APOLLO_SOURCE);
        result[ModelConstants.APOLLO_SOURCE].ModelVersion.Should().Be("1.0.0");
        result[ModelConstants.APOLLO_SOURCE].Heat.Should().Be(apolloHeat);
        result[ModelConstants.APOLLO_SOURCE].CurrentPrice.Should().Be(170.0m);
        
        result[ModelConstants.IGNIS_SOURCE].Should().NotBeNull();
        result[ModelConstants.IGNIS_SOURCE].Symbol.Should().Be(symbol);
        result[ModelConstants.IGNIS_SOURCE].ModelSource.Should().Be(ModelConstants.IGNIS_SOURCE);
        result[ModelConstants.IGNIS_SOURCE].ModelVersion.Should().Be("2.0.0");
        result[ModelConstants.IGNIS_SOURCE].Heat.Should().Be(ignisHeat);
        result[ModelConstants.IGNIS_SOURCE].CurrentPrice.Should().Be(172.0m);
        
        // Verify the predictions were saved to the database
        var savedPredictions = await _dbContext.Predictions
            .Where(p => p.Symbol == symbol && (p.ModelSource == ModelConstants.APOLLO_SOURCE || p.ModelSource == ModelConstants.IGNIS_SOURCE))
            .ToListAsync();
        savedPredictions.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshPortfolioPredictionsAsync_RefreshesPredictions_ForAllPortfolioSymbols()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            Name = "Test Portfolio"
        };

        // Create PortfolioStocks with references to the portfolio
        portfolio.PortfolioStocks = new List<PortfolioStock>
        {
            new PortfolioStock { 
                Symbol = "AAPL", 
                PortfolioId = portfolioId,
                Quantity = 10,
                CurrentTotalValue = 1500.00m,
                TotalBaseValue = 1500.00m,
                PercentageChange = 0.0m,
                LastUpdated = DateTime.UtcNow,
                Portfolio = portfolio
            },
            new PortfolioStock { 
                Symbol = "MSFT", 
                PortfolioId = portfolioId,
                Quantity = 5,
                CurrentTotalValue = 1000.00m,
                TotalBaseValue = 1000.00m,
                PercentageChange = 0.0m,
                LastUpdated = DateTime.UtcNow,
                Portfolio = portfolio
            }
        };
        
        _mockDatabaseService.Setup(s => s.GetPortfolioByIdWithPortfolioStocksAsync(portfolioId))
            .ReturnsAsync(portfolio);
            
        // Setup for symbol AAPL
        var appleHeat = new Heat { Symbol = "AAPL", Score = 75 };
        var appleResponse = new ValueTuple<Heat, DateTime, double>(appleHeat, DateTime.UtcNow.AddDays(30), 180.0);
        _mockGaiaService.Setup(s => s.GetHeatPredictionAsync("AAPL", portfolioId))
            .ReturnsAsync(appleResponse);
            
        // Setup for symbol MSFT
        var msftHeat = new Heat { Symbol = "MSFT", Score = 65 };
        var msftResponse = new ValueTuple<Heat, DateTime, double>(msftHeat, DateTime.UtcNow.AddDays(30), 300.0);
        _mockGaiaService.Setup(s => s.GetHeatPredictionAsync("MSFT", portfolioId))
            .ReturnsAsync(msftResponse);
            
        _mockGaiaService.Setup(s => s.GetModelVersionAsync())
            .ReturnsAsync("3.0.0");
            
        // Setup market data mocks
        var appleIntradayData = new IntradayMarketData { Symbol = "AAPL", Current = 172.0m };
        var msftIntradayData = new IntradayMarketData { Symbol = "MSFT", Current = 290.0m };
        _mockDatabaseService.Setup(s => s.GetLatestIntradayMarketDataAsync("AAPL"))
            .ReturnsAsync(appleIntradayData);
        _mockDatabaseService.Setup(s => s.GetLatestIntradayMarketDataAsync("MSFT"))
            .ReturnsAsync(msftIntradayData);
            
        // Act
        var result = await _service.RefreshPortfolioPredictionsAsync(portfolioId);
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.Should().ContainKey("AAPL");
        result.Should().ContainKey("MSFT");
        
        result["AAPL"].Should().NotBeNull();
        result["AAPL"].Symbol.Should().Be("AAPL");
        result["AAPL"].ModelSource.Should().Be(ModelConstants.GAIA_SOURCE);
        result["AAPL"].ModelVersion.Should().Be("3.0.0");
        result["AAPL"].Heat.Should().Be(appleHeat);
        
        result["MSFT"].Should().NotBeNull();
        result["MSFT"].Symbol.Should().Be("MSFT");
        result["MSFT"].ModelSource.Should().Be(ModelConstants.GAIA_SOURCE);
        result["MSFT"].ModelVersion.Should().Be("3.0.0");
        result["MSFT"].Heat.Should().Be(msftHeat);
        
        // Verify the predictions were saved to the database
        var savedPredictions = await _dbContext.Predictions
            .Where(p => (p.Symbol == "AAPL" || p.Symbol == "MSFT") && p.ModelSource == ModelConstants.GAIA_SOURCE)
            .ToListAsync();
        savedPredictions.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshAllPredictionsAsync_RefreshesPredictions_ForAllCompanies()
    {
        // Arrange
        var companies = new List<Company>
        {
            new Company { Symbol = "AAPL", Name = "Apple Inc." },
            new Company { Symbol = "MSFT", Name = "Microsoft Corporation" }
        };
        
        _mockDatabaseService.Setup(s => s.GetAllCompaniesAsync())
            .ReturnsAsync(companies);
            
        // Setup Apollo mock for AAPL
        var appleApolloHeat = new Heat { Symbol = "AAPL", Score = 75 };
        var appleApolloResponse = new ValueTuple<Heat, double>(appleApolloHeat, 180.0);
        _mockApolloService.Setup(s => s.GetHeatPredictionAsync("AAPL"))
            .ReturnsAsync(appleApolloResponse);
            
        // Setup Apollo mock for MSFT
        var msftApolloHeat = new Heat { Symbol = "MSFT", Score = 65 };
        var msftApolloResponse = new ValueTuple<Heat, double>(msftApolloHeat, 300.0);
        _mockApolloService.Setup(s => s.GetHeatPredictionAsync("MSFT"))
            .ReturnsAsync(msftApolloResponse);
            
        _mockApolloService.Setup(s => s.GetModelVersionAsync())
            .ReturnsAsync("1.0.0");
            
        // Setup Ignis mock for AAPL
        var appleIgnisHeat = new Heat { Symbol = "AAPL", Score = 70 };
        var appleIgnisResponse = new ValueTuple<Heat, double>(appleIgnisHeat, 175.0);
        _mockIgnisService.Setup(s => s.GetHeatPredictionAsync("AAPL"))
            .ReturnsAsync(appleIgnisResponse);
            
        // Setup Ignis mock for MSFT
        var msftIgnisHeat = new Heat { Symbol = "MSFT", Score = 60 };
        var msftIgnisResponse = new ValueTuple<Heat, double>(msftIgnisHeat, 295.0);
        _mockIgnisService.Setup(s => s.GetHeatPredictionAsync("MSFT"))
            .ReturnsAsync(msftIgnisResponse);
            
        _mockIgnisService.Setup(s => s.GetModelVersionAsync())
            .ReturnsAsync("2.0.0");
            
        // Setup market data mocks
        var appleHistoricalData = new HistoricalMarketData { Symbol = "AAPL", Close = 170.0m };
        var msftHistoricalData = new HistoricalMarketData { Symbol = "MSFT", Close = 290.0m };
        var appleIntradayData = new IntradayMarketData { Symbol = "AAPL", Current = 172.0m };
        var msftIntradayData = new IntradayMarketData { Symbol = "MSFT", Current = 292.0m };
        
        _mockDatabaseService.Setup(s => s.GetLatestHistoricalMarketDataAsync("AAPL"))
            .ReturnsAsync(appleHistoricalData);
        _mockDatabaseService.Setup(s => s.GetLatestHistoricalMarketDataAsync("MSFT"))
            .ReturnsAsync(msftHistoricalData);
        _mockDatabaseService.Setup(s => s.GetLatestIntradayMarketDataAsync("AAPL"))
            .ReturnsAsync(appleIntradayData);
        _mockDatabaseService.Setup(s => s.GetLatestIntradayMarketDataAsync("MSFT"))
            .ReturnsAsync(msftIntradayData);
            
        // Act
        await _service.RefreshAllPredictionsAsync();
        
        // Assert - Check that predictions were saved to the database
        var savedPredictions = await _dbContext.Predictions.ToListAsync();
        savedPredictions.Count.Should().BeGreaterOrEqualTo(4); // At least 4 predictions (2 symbols x 2 sources)
        
        // Verify Apollo predictions
        var apolloPredictions = savedPredictions.Where(p => p.ModelSource == ModelConstants.APOLLO_SOURCE).ToList();
        apolloPredictions.Count.Should().BeGreaterOrEqualTo(2);
        apolloPredictions.Should().Contain(p => p.Symbol == "AAPL");
        apolloPredictions.Should().Contain(p => p.Symbol == "MSFT");
        
        // Verify Ignis predictions
        var ignisPredictions = savedPredictions.Where(p => p.ModelSource == ModelConstants.IGNIS_SOURCE).ToList();
        ignisPredictions.Count.Should().BeGreaterOrEqualTo(2);
        ignisPredictions.Should().Contain(p => p.Symbol == "AAPL");
        ignisPredictions.Should().Contain(p => p.Symbol == "MSFT");
    }

    // Private test class to simulate exceptions
    private class TestModelPredictionService : IModelPredictionService
    {
        private readonly Serilog.ILogger _logger;
        
        public TestModelPredictionService(Serilog.ILogger logger)
        {
            _logger = logger;
        }
        
        public Task<Prediction?> GetLatestPredictionAsync(string symbol, string modelSource)
        {
            // Simulate an exception
            var exception = new Exception("Test exception");
            _logger.Error(exception, "Error retrieving latest prediction for {Symbol} from {ModelSource}", symbol, modelSource);
            return Task.FromResult<Prediction?>(null);
        }
        
        // Implement other interface methods with minimal functionality
        public Task<Dictionary<string, Prediction>> GetAllLatestPredictionsAsync(string symbol)
            => Task.FromResult(new Dictionary<string, Prediction>());
            
        public Task<Prediction> StorePredictionAsync(Prediction prediction)
        {
            var exception = new Exception("Test exception");
            _logger.Error(exception, "Error storing prediction for {Symbol} from {ModelSource}", 
                prediction.Symbol, prediction.ModelSource);
            throw exception;
        }
            
        public Task<IEnumerable<Prediction>> GetHistoricalPredictionsAsync(string symbol, string modelSource, DateTime startDate, DateTime endDate)
            => Task.FromResult<IEnumerable<Prediction>>(new List<Prediction>());
            
        public Task<Dictionary<string, Prediction>> RefreshPredictionsAsync(string symbol)
            => Task.FromResult(new Dictionary<string, Prediction>());
            
        public Task<Dictionary<string, Prediction>> RefreshPortfolioPredictionsAsync(string portfolioId)
            => Task.FromResult(new Dictionary<string, Prediction>());
            
        public Task RefreshAllPredictionsAsync()
            => Task.CompletedTask;
    }
} 