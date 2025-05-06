using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.Test.Unit.Utilities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class PortfolioControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<IPortfolioService> _portfolioServiceMock;
    private readonly PortfolioController _controller;
    private readonly User _testUser;

    public PortfolioControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _portfolioServiceMock = new Mock<IPortfolioService>();
        _controller = new PortfolioController(_dbServiceMock.Object, _portfolioServiceMock.Object);
        
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
        var httpContext = new DefaultHttpContext();
        httpContext.Items = new Dictionary<object, object?>
        {
            { "User", _testUser },
            { "ServiceAccount", null }
        };
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetUserPortfolios_ReturnsPortfolios()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new Portfolio { Id = "portfolio1", UserId = "user1", Name = "Portfolio 1" },
            new Portfolio { Id = "portfolio2", UserId = "user1", Name = "Portfolio 2" }
        };
        
        _dbServiceMock.Setup(db => db.GetUserPortfoliosAsync(_testUser.Id))
            .ReturnsAsync(portfolios);
        
        // Act
        var result = await _controller.GetUserPortfolios();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultPortfolios = Assert.IsAssignableFrom<IEnumerable<Portfolio>>(okResult.Value);
        Assert.Equal(2, resultPortfolios.Count());
    }

    [Fact]
    public async Task GetPortfolio_ExistingPortfolio_ReturnsPortfolio()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var portfolio = new Portfolio 
        { 
            Id = portfolioId, 
            UserId = _testUser.Id, 
            Name = "Test Portfolio" 
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        // Act
        var result = await _controller.GetPortfolio(portfolioId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultPortfolio = Assert.IsType<Portfolio>(okResult.Value);
        Assert.Equal(portfolioId, resultPortfolio.Id);
    }

    [Fact]
    public async Task GetPortfolio_NonExistingPortfolio_ReturnsNotFound()
    {
        // Arrange
        var portfolioId = "nonexistent";
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio)null);
        
        // Act
        var result = await _controller.GetPortfolio(portfolioId);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task GetPortfolio_OtherUserPortfolio_ReturnsForbid()
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
        var result = await _controller.GetPortfolio(portfolioId);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetPortfolio_AdminAccessingOtherPortfolio_ReturnsOk()
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
        
        var httpContext = new DefaultHttpContext();
        httpContext.Items = new Dictionary<object, object?>
        {
            { "User", adminUser },
            { "ServiceAccount", null }
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
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
        
        // Act
        var result = await _controller.GetPortfolio(portfolioId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultPortfolio = Assert.IsType<Portfolio>(okResult.Value);
        Assert.Equal(portfolioId, resultPortfolio.Id);
    }

    [Fact]
    public async Task CreatePortfolio_ReturnsCreatedPortfolio()
    {
        // Arrange
        var request = new PortfolioController.CreatePortfolioRequest
        {
            Name = "New Portfolio",
            StrategyDescription = PortfolioStrategy.Balanced
        };
        
        var createdPortfolio = new Portfolio
        {
            Id = "newPortfolio",
            UserId = _testUser.Id,
            Name = request.Name,
            StrategyDescription = request.StrategyDescription
        };
        
        _dbServiceMock.Setup(db => db.CreatePortfolioAsync(It.IsAny<Portfolio>()))
            .ReturnsAsync(createdPortfolio);
        
        // Act
        var result = await _controller.CreatePortfolio(request);
        
        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetPortfolio", createdAtActionResult.ActionName);
        Assert.Equal(createdPortfolio.Id, createdAtActionResult.RouteValues["id"]);
        
        var resultPortfolio = Assert.IsType<Portfolio>(createdAtActionResult.Value);
        Assert.Equal(createdPortfolio.Id, resultPortfolio.Id);
        Assert.Equal(_testUser.Id, resultPortfolio.UserId);
        Assert.Equal(request.Name, resultPortfolio.Name);
    }

    [Fact]
    public async Task UpdatePortfolio_ExistingPortfolio_ReturnsUpdatedPortfolio()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Original Name",
            StrategyDescription = PortfolioStrategy.Conservative
        };
        
        var request = new PortfolioController.UpdatePortfolioRequest
        {
            Name = "Updated Name",
            StrategyDescription = PortfolioStrategy.Aggressive
        };
        
        var updatedPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = request.Name,
            StrategyDescription = request.StrategyDescription
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        _dbServiceMock.Setup(db => db.UpdatePortfolioAsync(It.IsAny<Portfolio>()))
            .ReturnsAsync(updatedPortfolio);
        
        // Act
        var result = await _controller.UpdatePortfolio(portfolioId, request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultPortfolio = Assert.IsType<Portfolio>(okResult.Value);
        Assert.Equal(portfolioId, resultPortfolio.Id);
        Assert.Equal(request.Name, resultPortfolio.Name);
        Assert.Equal(request.StrategyDescription, resultPortfolio.StrategyDescription);
    }

    [Fact]
    public async Task UpdatePortfolio_NonExistingPortfolio_ReturnsNotFound()
    {
        // Arrange
        var portfolioId = "nonexistent";
        var request = new PortfolioController.UpdatePortfolioRequest
        {
            Name = "Updated Name",
            StrategyDescription = PortfolioStrategy.Aggressive
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio)null);
        
        // Act
        var result = await _controller.UpdatePortfolio(portfolioId, request);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task UpdatePortfolio_OtherUserPortfolio_ReturnsForbid()
    {
        // Arrange
        var portfolioId = "portfolio2";
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio",
            StrategyDescription = PortfolioStrategy.Conservative
        };
        
        var request = new PortfolioController.UpdatePortfolioRequest
        {
            Name = "Updated Name",
            StrategyDescription = PortfolioStrategy.Aggressive
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        // Act
        var result = await _controller.UpdatePortfolio(portfolioId, request);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeletePortfolio_ExistingPortfolio_ReturnsSuccess()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Portfolio to Delete"
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        _dbServiceMock.Setup(db => db.DeletePortfolioAsync(portfolioId))
            .ReturnsAsync(true);
        
        // Act
        var result = await _controller.DeletePortfolio(portfolioId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        TestFactory.AssertResponseMessage(result, "Portfolio deleted successfully");
    }

    [Fact]
    public async Task DeletePortfolio_NonExistingPortfolio_ReturnsNotFound()
    {
        // Arrange
        var portfolioId = "nonexistent";
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio)null);
        
        // Act
        var result = await _controller.DeletePortfolio(portfolioId);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task DeletePortfolio_OtherUserPortfolio_ReturnsForbid()
    {
        // Arrange
        var portfolioId = "portfolio2";
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        // Act
        var result = await _controller.DeletePortfolio(portfolioId);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeletePortfolio_DeletionFailed_ReturnsInternalServerError()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Portfolio to Delete"
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        _dbServiceMock.Setup(db => db.DeletePortfolioAsync(portfolioId))
            .ReturnsAsync(false);
        
        // Act
        var result = await _controller.DeletePortfolio(portfolioId);
        
        // Assert
        TestFactory.AssertInternalServerErrorResult(result);
    }

    [Fact]
    public async Task GeneratePortfolioPerformanceReport_ExistingPortfolio_ReturnsFileResult()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;
        
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var pdfBytes = new byte[] { 1, 2, 3, 4, 5 }; // Mock PDF data
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        _portfolioServiceMock.Setup(s => s.GeneratePortfolioPerformancePdfAsync(
                _testUser.Id, portfolioId, startDate, endDate))
            .ReturnsAsync(pdfBytes);
        
        // Act
        var result = await _controller.GeneratePortfolioPerformanceReport(portfolioId, startDate, endDate);
        
        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains($"portfolio-performance-report-{portfolioId}", fileResult.FileDownloadName);
        Assert.Equal(pdfBytes, fileResult.FileContents);
    }

    [Fact]
    public async Task GeneratePortfolioPerformanceReport_NonExistingPortfolio_ReturnsNotFound()
    {
        // Arrange
        var portfolioId = "nonexistent";
        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio)null);
        
        // Act
        var result = await _controller.GeneratePortfolioPerformanceReport(portfolioId, startDate, endDate);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task GeneratePortfolioPerformanceReport_OtherUserPortfolio_ReturnsForbid()
    {
        // Arrange
        var portfolioId = "portfolio2";
        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;
        
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = "otherUser",
            Name = "Other User Portfolio"
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        // Act
        var result = await _controller.GeneratePortfolioPerformanceReport(portfolioId, startDate, endDate);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GeneratePortfolioPerformanceReport_PdfGenerationFailure_ReturnsInternalServerError()
    {
        // Arrange
        var portfolioId = "portfolio1";
        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;
        
        var existingPortfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        _dbServiceMock.Setup(db => db.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(existingPortfolio);
        
        _portfolioServiceMock.Setup(s => s.GeneratePortfolioPerformancePdfAsync(
                _testUser.Id, portfolioId, startDate, endDate))
            .ThrowsAsync(new Exception("PDF generation failed"));
        
        // Act
        var result = await _controller.GeneratePortfolioPerformanceReport(portfolioId, startDate, endDate);
        
        // Assert
        TestFactory.AssertInternalServerErrorResult(result);
    }
} 