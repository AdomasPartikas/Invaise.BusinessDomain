using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.FinnhubAPIClient;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class MarketDataServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly MarketDataService _service;
    private readonly Mock<IFinnhubClient> _finnhubClientMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IKaggleService> _kaggleServiceMock;
    private readonly Mock<IDataService> _dataServiceMock;
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;
    private readonly DbContextOptions<InvaiseDbContext> _options;

    public MarketDataServiceTests()
    {
        // Setup in-memory database
        _options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: $"MarketDataServiceTestDb_{Guid.NewGuid()}")
            .Options;

        // Create DbContext with in-memory database
        _dbContext = new InvaiseDbContext(_options);
        
        // Create test data
        SeedDatabase();
        
        // Setup mocks
        _finnhubClientMock = new Mock<IFinnhubClient>();
        _mapperMock = new Mock<IMapper>();
        _kaggleServiceMock = new Mock<IKaggleService>();
        _dataServiceMock = new Mock<IDataService>();
        _dbServiceMock = new Mock<IDatabaseService>();
        _loggerMock = new Mock<Serilog.ILogger>();
        
        // Create service with mocks and real DbContext
        _service = new MarketDataService(
            _finnhubClientMock.Object,
            _mapperMock.Object,
            _dbContext,
            _kaggleServiceMock.Object,
            _dataServiceMock.Object,
            _dbServiceMock.Object,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        // Add sample market data
        _dbContext.HistoricalMarketData.AddRange(new List<HistoricalMarketData>
        {
            new HistoricalMarketData 
            { 
                Id = 1, 
                Symbol = "AAPL", 
                Date = DateTime.UtcNow.AddDays(-3),
                Open = 150.0m,
                High = 155.0m,
                Low = 149.0m,
                Close = 153.0m,
                Volume = 1000000
            },
            new HistoricalMarketData 
            { 
                Id = 2, 
                Symbol = "MSFT", 
                Date = DateTime.UtcNow.AddDays(-3),
                Open = 250.0m,
                High = 255.0m,
                Low = 248.0m,
                Close = 252.0m,
                Volume = 800000
            }
        });
        
        // Add sample intraday data
        _dbContext.IntradayMarketData.AddRange(new List<IntradayMarketData>
        {
            new IntradayMarketData 
            { 
                Id = 1, 
                Symbol = "AAPL", 
                Timestamp = DateTime.UtcNow.AddHours(-3),
                Open = 153.0m,
                High = 154.0m,
                Low = 152.0m,
                Current = 153.5m
            },
            new IntradayMarketData 
            { 
                Id = 2, 
                Symbol = "MSFT", 
                Timestamp = DateTime.UtcNow.AddHours(-3),
                Open = 252.0m,
                High = 253.0m,
                Low = 251.5m,
                Current = 252.5m
            }
        });
        
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task IsMarketOpenAsync_ReturnsTrue_WhenMarketIsOpen()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = true
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);

        // Act
        var result = await _service.IsMarketOpenAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMarketOpenAsync_ReturnsFalse_WhenMarketIsClosed()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = false
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);

        // Act
        var result = await _service.IsMarketOpenAsync();

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task IsMarketOpenAsync_ReturnsFalse_WhenExceptionIsThrown()
    {
        // Arrange
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ThrowsAsync(new FinnhubAPIClientException("Error", 500, "Error message", null, null));

        // Act
        var result = await _service.IsMarketOpenAsync();

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task ImportIntradayMarketDataAsync_DoesNothing_WhenMarketIsClosed()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = false
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);
            
        var initialCount = await _dbContext.IntradayMarketData.CountAsync();

        // Act
        await _service.ImportIntradayMarketDataAsync();

        // Assert
        var finalCount = await _dbContext.IntradayMarketData.CountAsync();
        Assert.Equal(initialCount, finalCount); // No new records should be added
    }
    
    [Fact]
    public async Task ImportIntradayMarketDataAsync_ImportsData_WhenMarketIsOpen()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = true
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);
            
        var symbols = new List<string> { "AAPL", "MSFT" };
        
        _dbServiceMock
            .Setup(service => service.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(symbols);
            
        var appleQuote = new Quote
        {
            O = 154.0f, // Open price
            H = 156.0f, // High price
            L = 153.0f, // Low price
            C = 155.0f, // Current price
            Pc = 153.0f, // Previous close
            T = 1625086800 // Timestamp
        };
        
        var msftQuote = new Quote
        {
            O = 259.0f,
            H = 262.0f,
            L = 258.0f,
            C = 260.0f,
            Pc = 257.0f,
            T = 1625086800
        };
        
        var googQuote = new Quote
        {
            O = 1599.0f,
            H = 1602.0f,
            L = 1598.0f,
            C = 1600.0f,
            Pc = 1595.0f,
            T = 1625086800
        };
        
        _finnhubClientMock
            .Setup(client => client.QuoteAsync("AAPL"))
            .ReturnsAsync(appleQuote);
            
        _finnhubClientMock
            .Setup(client => client.QuoteAsync("MSFT"))
            .ReturnsAsync(msftQuote);
            
        _finnhubClientMock
            .Setup(client => client.QuoteAsync("GOOG"))
            .ReturnsAsync(googQuote);
            
        var appleData = new IntradayMarketData { 
            Symbol = "AAPL", 
            Open = 159.0m,
            High = 160.5m,
            Low = 158.5m,
            Current = 160.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        var msftData = new IntradayMarketData { 
            Symbol = "MSFT", 
            Open = 259.0m,
            High = 260.5m,
            Low = 258.5m,
            Current = 260.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        var googData = new IntradayMarketData { 
            Symbol = "GOOG", 
            Open = 1599.0m,
            High = 1600.5m,
            Low = 1598.5m,
            Current = 1600.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        _mapperMock
            .Setup(mapper => mapper.Map<IntradayMarketData>(appleQuote))
            .Returns(appleData);
            
        _mapperMock
            .Setup(mapper => mapper.Map<IntradayMarketData>(msftQuote))
            .Returns(msftData);
            
        _mapperMock
            .Setup(mapper => mapper.Map<IntradayMarketData>(googQuote))
            .Returns(googData);
            
        var initialCount = await _dbContext.IntradayMarketData.CountAsync();

        // Act
        await _service.ImportIntradayMarketDataAsync();

        // Assert
        var finalCount = await _dbContext.IntradayMarketData.CountAsync();
        Assert.Equal(initialCount + 2, finalCount); // Two new records should be added
    }
} 