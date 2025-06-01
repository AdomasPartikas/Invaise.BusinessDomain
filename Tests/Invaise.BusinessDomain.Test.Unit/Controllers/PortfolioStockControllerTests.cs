using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.Test.Unit.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class PortfolioStockControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly PortfolioStockController _controller;
    private readonly User _testUser;

    public PortfolioStockControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _controller = new PortfolioStockController(_dbServiceMock.Object);
        
        // Set up a test user
        _testUser = new User
        {
            Id = "user1",
            DisplayName = "Test User",
            Email = "test@example.com",
            Role = "User",
            IsActive = true
        };

        // Set up HttpContext
        var httpContext = new DefaultHttpContext
        {
            Items = new Dictionary<object, object?>
        {
            { "User", _testUser },
            { "ServiceAccount", null }
        }
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetPortfolioStocks_ExistingPortfolio_ReturnsStocks()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var stocks = new List<PortfolioStock>
        {
            new() { 
                ID = "stock1", 
                PortfolioId = portfolioId, 
                Symbol = "AAPL", 
                Quantity = 10,
                CurrentTotalValue = 1500,
                TotalBaseValue = 1400,
                PercentageChange = 7.14m,
                LastUpdated = DateTime.UtcNow,
                Portfolio = portfolio
            },
            new() { 
                ID = "stock2", 
                PortfolioId = portfolioId, 
                Symbol = "MSFT", 
                Quantity = 5,
                CurrentTotalValue = 1000,
                TotalBaseValue = 900,
                PercentageChange = 11.11m,
                LastUpdated = DateTime.UtcNow,
                Portfolio = portfolio
            }
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        _dbServiceMock.Setup(db => db.GetPortfolioStocksAsync(portfolioId))
            .ReturnsAsync(stocks);
        
        // Act
        var result = await _controller.GetPortfolioStocks(portfolioId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultStocks = Assert.IsAssignableFrom<IEnumerable<PortfolioStock>>(okResult.Value);
        Assert.Equal(2, resultStocks.Count());
    }

    [Fact]
    public async Task GetPortfolioStocks_NonExistingPortfolio_ReturnsNotFound()
    {
        // Arrange
        var portfolioId = "nonexistent";
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio?)null);
        
        // Act
        var result = await _controller.GetPortfolioStocks(portfolioId);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task GetPortfolioStocks_OtherUserPortfolio_ReturnsForbid()
    {
        // Arrange
        var portfolioId = "portfolio2";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        // Act
        var result = await _controller.GetPortfolioStocks(portfolioId);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetPortfolioStocks_AdminAccessingOtherPortfolio_ReturnsOk()
    {
        // Arrange
        var adminUser = new User
        {
            Id = "admin1",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Role = "Admin",
            IsActive = true
        };

        var httpContext = new DefaultHttpContext
        {
            Items = new Dictionary<object, object?>
        {
            { "User", adminUser },
            { "ServiceAccount", null }
        }
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        var portfolioId = "portfolio2";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        var stocks = new List<PortfolioStock>
        {
            new() { 
                ID = "stock1", 
                PortfolioId = portfolioId, 
                Symbol = "AAPL", 
                Quantity = 10,
                CurrentTotalValue = 1500,
                TotalBaseValue = 1400,
                PercentageChange = 7.14m,
                LastUpdated = DateTime.UtcNow,
                Portfolio = portfolio
            }
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        _dbServiceMock.Setup(db => db.GetPortfolioStocksAsync(portfolioId))
            .ReturnsAsync(stocks);
        
        // Act
        var result = await _controller.GetPortfolioStocks(portfolioId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultStocks = Assert.IsAssignableFrom<IEnumerable<PortfolioStock>>(okResult.Value);
        Assert.Single(resultStocks);
    }

    [Fact]
    public async Task GetPortfolioStock_ExistingStock_ReturnsStock()
    {
        // Arrange
        var stockId = "stock1";
        var portfolioId = "portfolio1";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var stock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(stock);
        
        // Act
        var result = await _controller.GetPortfolioStock(stockId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultStock = Assert.IsType<PortfolioStock>(okResult.Value);
        Assert.Equal(stockId, resultStock.ID);
    }

    [Fact]
    public async Task GetPortfolioStock_NonExistingStock_ReturnsNotFound()
    {
        // Arrange
        var stockId = "nonexistent";
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync((PortfolioStock?)null);
        
        // Act
        var result = await _controller.GetPortfolioStock(stockId);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task GetPortfolioStock_OtherUserStock_ReturnsForbid()
    {
        // Arrange
        var stockId = "stock2";
        var portfolioId = "portfolio2";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        var stock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(stock);
        
        // Act
        var result = await _controller.GetPortfolioStock(stockId);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AddStockToPortfolio_NewStock_ReturnsCreatedStock()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var request = new PortfolioStockController.CreatePortfolioStockRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m
        };

        var existingStocks = new List<PortfolioStock>();
        
        var createdStock = new PortfolioStock
        {
            ID = "stock1",
            PortfolioId = portfolioId,
            Symbol = request.Symbol,
            Quantity = request.Quantity,
            CurrentTotalValue = request.CurrentTotalValue,
            TotalBaseValue = request.TotalBaseValue,
            PercentageChange = request.PercentageChange,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        _dbServiceMock.Setup(db => db.GetPortfolioStocksAsync(portfolioId))
            .ReturnsAsync(existingStocks);
        
        _dbServiceMock.Setup(db => db.AddPortfolioStockAsync(It.IsAny<PortfolioStock>()))
            .ReturnsAsync(createdStock);
        
        // Act
        var result = await _controller.AddStockToPortfolio(request);
        
        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetPortfolioStock", createdAtActionResult.ActionName);
        Assert.Equal(createdStock.ID, createdAtActionResult.RouteValues!["id"]);
        
        var resultStock = Assert.IsType<PortfolioStock>(createdAtActionResult.Value);
        Assert.Equal(createdStock.ID, resultStock.ID);
        Assert.Equal(request.Symbol, resultStock.Symbol);
        Assert.Equal(request.Quantity, resultStock.Quantity);
    }

    [Fact]
    public async Task AddStockToPortfolio_ExistingStock_ReturnsUpdatedStock()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var stockId = "stock1";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var request = new PortfolioStockController.CreatePortfolioStockRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 5,
            CurrentTotalValue = 750,
            TotalBaseValue = 700,
            PercentageChange = 7.14m
        };

        var existingStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = request.Symbol,
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        var existingStocks = new List<PortfolioStock> { existingStock };
        
        var updatedStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = request.Symbol,
            Quantity = 15, // 10 + 5
            CurrentTotalValue = 2250, // 1500 + 750
            TotalBaseValue = 2100, // 1400 + 700
            PercentageChange = request.PercentageChange,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        _dbServiceMock.Setup(db => db.GetPortfolioStocksAsync(portfolioId))
            .ReturnsAsync(existingStocks);
        
        _dbServiceMock.Setup(db => db.UpdatePortfolioStockAsync(It.IsAny<PortfolioStock>()))
            .ReturnsAsync(updatedStock);
        
        // Act
        var result = await _controller.AddStockToPortfolio(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultStock = Assert.IsType<PortfolioStock>(okResult.Value);
        Assert.Equal(updatedStock.ID, resultStock.ID);
        Assert.Equal(updatedStock.Quantity, resultStock.Quantity);
        Assert.Equal(updatedStock.CurrentTotalValue, resultStock.CurrentTotalValue);
        Assert.Equal(updatedStock.TotalBaseValue, resultStock.TotalBaseValue);
    }

    [Fact]
    public async Task AddStockToPortfolio_NonExistingPortfolio_ReturnsNotFound()
    {
        // Arrange
        var portfolioId = "nonexistent";
        
        var request = new PortfolioStockController.CreatePortfolioStockRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio?)null);
        
        // Act
        var result = await _controller.AddStockToPortfolio(request);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task AddStockToPortfolio_OtherUserPortfolio_ReturnsForbid()
    {
        // Arrange
        var portfolioId = "portfolio2";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        var request = new PortfolioStockController.CreatePortfolioStockRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        // Act
        var result = await _controller.AddStockToPortfolio(request);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdatePortfolioStock_ExistingStock_ReturnsUpdatedStock()
    {
        // Arrange
        var stockId = "stock1";
        var portfolioId = "portfolio1";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var existingStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        var request = new PortfolioStockController.UpdatePortfolioStockRequest
        {
            Quantity = 15,
            CurrentTotalValue = 2250,
            TotalBaseValue = 2100,
            PercentageChange = 7.14m
        };
        
        var updatedStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = request.Quantity.Value,
            CurrentTotalValue = request.CurrentTotalValue.Value,
            TotalBaseValue = request.TotalBaseValue.Value,
            PercentageChange = request.PercentageChange.Value,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(existingStock);
        
        _dbServiceMock.Setup(db => db.UpdatePortfolioStockAsync(It.IsAny<PortfolioStock>()))
            .ReturnsAsync(updatedStock);
        
        // Act
        var result = await _controller.UpdatePortfolioStock(stockId, request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultStock = Assert.IsType<PortfolioStock>(okResult.Value);
        Assert.Equal(stockId, resultStock.ID);
        Assert.Equal(request.Quantity, resultStock.Quantity);
        Assert.Equal(request.CurrentTotalValue, resultStock.CurrentTotalValue);
        Assert.Equal(request.TotalBaseValue, resultStock.TotalBaseValue);
        Assert.Equal(request.PercentageChange, resultStock.PercentageChange);
    }

    [Fact]
    public async Task UpdatePortfolioStock_NonExistingStock_ReturnsNotFound()
    {
        // Arrange
        var stockId = "nonexistent";
        
        var request = new PortfolioStockController.UpdatePortfolioStockRequest
        {
            Quantity = 15,
            CurrentTotalValue = 2250,
            TotalBaseValue = 2100,
            PercentageChange = 7.14m
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync((PortfolioStock?)null);
        
        // Act
        var result = await _controller.UpdatePortfolioStock(stockId, request);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task UpdatePortfolioStock_OtherUserStock_ReturnsForbid()
    {
        // Arrange
        var stockId = "stock2";
        var portfolioId = "portfolio2";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        var existingStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        var request = new PortfolioStockController.UpdatePortfolioStockRequest
        {
            Quantity = 15,
            CurrentTotalValue = 2250,
            TotalBaseValue = 2100,
            PercentageChange = 7.14m
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(existingStock);
        
        // Act
        var result = await _controller.UpdatePortfolioStock(stockId, request);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeletePortfolioStock_ExistingStock_ReturnsSuccess()
    {
        // Arrange
        var stockId = "stock1";
        var portfolioId = "portfolio1";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var existingStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(existingStock);
        
        _dbServiceMock.Setup(db => db.DeletePortfolioStockAsync(stockId))
            .ReturnsAsync(true);
        
        // Act
        var result = await _controller.DeletePortfolioStock(stockId);
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
        TestFactory.AssertResponseMessage(result, "Portfolio stock deleted successfully");
    }

    [Fact]
    public async Task DeletePortfolioStock_NonExistingStock_ReturnsNotFound()
    {
        // Arrange
        var stockId = "nonexistent";
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync((PortfolioStock?)null);
        
        // Act
        var result = await _controller.DeletePortfolioStock(stockId);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task DeletePortfolioStock_OtherUserStock_ReturnsForbid()
    {
        // Arrange
        var stockId = "stock2";
        var portfolioId = "portfolio2";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        var existingStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(existingStock);
        
        // Act
        var result = await _controller.DeletePortfolioStock(stockId);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeletePortfolioStock_DeletionFailed_ReturnsInternalServerError()
    {
        // Arrange
        var stockId = "stock1";
        var portfolioId = "portfolio1";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var existingStock = new PortfolioStock
        {
            ID = stockId,
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            CurrentTotalValue = 1500,
            TotalBaseValue = 1400,
            PercentageChange = 7.14m,
            LastUpdated = DateTime.UtcNow,
            Portfolio = portfolio
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioStockByIdAsync(stockId))
            .ReturnsAsync(existingStock);
        
        _dbServiceMock.Setup(db => db.DeletePortfolioStockAsync(stockId))
            .ReturnsAsync(false);
        
        // Act
        var result = await _controller.DeletePortfolioStock(stockId);
        
        // Assert
        TestFactory.AssertInternalServerErrorResult(result);
    }
} 