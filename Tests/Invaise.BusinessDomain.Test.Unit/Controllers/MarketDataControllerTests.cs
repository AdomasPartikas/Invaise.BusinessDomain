using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class MarketDataControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<IMarketDataService> _marketDataServiceMock;
    private readonly MarketDataController _controller;

    public MarketDataControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _marketDataServiceMock = new Mock<IMarketDataService>();
        _controller = new MarketDataController(_dbServiceMock.Object, _marketDataServiceMock.Object);
    }

    [Fact]
    public async Task GetHistoricalMarketData_ValidSymbol_ReturnsOkWithData()
    {
        // Arrange
        string symbol = "AAPL";
        DateTime? start = DateTime.Now.AddDays(-30);
        DateTime? end = DateTime.Now;
        
        var expectedData = new List<HistoricalMarketData>
        {
            new HistoricalMarketData { Symbol = symbol, Date = DateTime.Now.AddDays(-2) },
            new HistoricalMarketData { Symbol = symbol, Date = DateTime.Now.AddDays(-1) }
        };
        
        _dbServiceMock.Setup(s => s.GetHistoricalMarketDataAsync(symbol, start, end))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetHistoricalMarketData(symbol, start, end);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsAssignableFrom<IEnumerable<HistoricalMarketData>>(okResult.Value);
        Assert.Equal(2, returnedData.Count());
    }

    [Fact]
    public async Task GetHistoricalMarketData_EmptySymbol_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "";
        DateTime? start = DateTime.Now.AddDays(-30);
        DateTime? end = DateTime.Now;

        // Act
        var result = await _controller.GetHistoricalMarketData(symbol, start, end);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Symbol is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetIntradayMarketData_ValidSymbol_ReturnsOkWithData()
    {
        // Arrange
        string symbol = "AAPL";
        DateTime? start = DateTime.Now.AddDays(-1);
        DateTime? end = DateTime.Now;
        
        var expectedData = new List<IntradayMarketData>
        {
            new IntradayMarketData { Symbol = symbol, Timestamp = DateTime.Now.AddHours(-2) },
            new IntradayMarketData { Symbol = symbol, Timestamp = DateTime.Now.AddHours(-1) }
        };
        
        _dbServiceMock.Setup(s => s.GetIntradayMarketDataAsync(symbol, start, end))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetIntradayMarketData(symbol, start, end);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsAssignableFrom<IEnumerable<IntradayMarketData>>(okResult.Value);
        Assert.Equal(2, returnedData.Count());
    }

    [Fact]
    public async Task GetIntradayMarketData_EmptySymbol_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "";
        DateTime? start = DateTime.Now.AddDays(-1);
        DateTime? end = DateTime.Now;

        // Act
        var result = await _controller.GetIntradayMarketData(symbol, start, end);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Symbol is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetLatestIntradayMarketData_ValidSymbol_ReturnsOkWithData()
    {
        // Arrange
        string symbol = "AAPL";
        var expectedData = new IntradayMarketData { Symbol = symbol, Timestamp = DateTime.Now };
        
        _dbServiceMock.Setup(s => s.GetLatestIntradayMarketDataAsync(symbol))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetLatestIntradayMarketData(symbol);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<IntradayMarketData>(okResult.Value);
        Assert.Equal(symbol, returnedData.Symbol);
    }

    [Fact]
    public async Task GetLatestIntradayMarketData_EmptySymbol_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "";

        // Act
        var result = await _controller.GetLatestIntradayMarketData(symbol);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Symbol is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataWithCount_ValidParameters_ReturnsOkWithData()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 5;
        
        var expectedData = new List<IntradayMarketData>
        {
            new IntradayMarketData { Symbol = symbol, Timestamp = DateTime.Now.AddHours(-2) },
            new IntradayMarketData { Symbol = symbol, Timestamp = DateTime.Now.AddHours(-1) }
        };
        
        _dbServiceMock.Setup(s => s.GetLatestIntradayMarketDataAsync(symbol, count))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetLatestIntradayMarketDataWithCount(symbol, count);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsAssignableFrom<IEnumerable<IntradayMarketData>>(okResult.Value);
        Assert.Equal(2, returnedData.Count());
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataWithCount_EmptySymbol_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "";
        int count = 5;

        // Act
        var result = await _controller.GetLatestIntradayMarketDataWithCount(symbol, count);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Symbol is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetLatestIntradayMarketDataWithCount_InvalidCount_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 0;

        // Act
        var result = await _controller.GetLatestIntradayMarketDataWithCount(symbol, count);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Count must be greater than zero.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketData_ValidSymbol_ReturnsOkWithData()
    {
        // Arrange
        string symbol = "AAPL";
        var expectedData = new HistoricalMarketData { Symbol = symbol, Date = DateTime.Now };
        
        _dbServiceMock.Setup(s => s.GetLatestHistoricalMarketDataAsync(symbol))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetLatestHistoricalMarketData(symbol);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<HistoricalMarketData>(okResult.Value);
        Assert.Equal(symbol, returnedData.Symbol);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketData_EmptySymbol_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "";

        // Act
        var result = await _controller.GetLatestHistoricalMarketData(symbol);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Symbol is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataWithCount_ValidParameters_ReturnsOkWithData()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 5;
        
        var expectedData = new List<HistoricalMarketData>
        {
            new HistoricalMarketData { Symbol = symbol, Date = DateTime.Now.AddDays(-2) },
            new HistoricalMarketData { Symbol = symbol, Date = DateTime.Now.AddDays(-1) }
        };
        
        _dbServiceMock.Setup(s => s.GetLatestHistoricalMarketDataAsync(symbol, count))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetLatestHistoricalMarketDataWithCount(symbol, count);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsAssignableFrom<IEnumerable<HistoricalMarketData>>(okResult.Value);
        Assert.NotNull(returnedData);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataWithCount_EmptySymbol_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "";
        int count = 5;

        // Act
        var result = await _controller.GetLatestHistoricalMarketDataWithCount(symbol, count);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Symbol is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetLatestHistoricalMarketDataWithCount_InvalidCount_ReturnsBadRequest()
    {
        // Arrange
        string symbol = "AAPL";
        int count = 0;

        // Act
        var result = await _controller.GetLatestHistoricalMarketDataWithCount(symbol, count);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Count must be greater than zero.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetAllUniqueSymbols_HasSymbols_ReturnsOkWithSymbols()
    {
        // Arrange
        var expectedSymbols = new List<string> { "AAPL", "MSFT", "GOOG" };
        
        _dbServiceMock.Setup(s => s.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(expectedSymbols);

        // Act
        var result = await _controller.GetAllUniqueSymbols();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSymbols = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Equal(3, returnedSymbols.Count());
    }

    [Fact]
    public async Task GetAllUniqueSymbols_NoSymbols_ReturnsNotFound()
    {
        // Arrange
        _dbServiceMock.Setup(s => s.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.GetAllUniqueSymbols();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("No symbols found.", notFoundResult.Value);
    }

    [Fact]
    public async Task IsMarketOpen_ReturnsOkWithStatus()
    {
        // Arrange
        bool isOpen = true;
        
        _marketDataServiceMock.Setup(s => s.IsMarketOpenAsync())
            .ReturnsAsync(isOpen);

        // Act
        var result = await _controller.IsMarketOpen();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStatus = Assert.IsType<bool>(okResult.Value);
        Assert.True(returnedStatus);
    }
} 