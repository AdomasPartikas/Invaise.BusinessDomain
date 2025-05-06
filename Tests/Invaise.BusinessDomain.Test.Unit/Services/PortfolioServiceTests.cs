using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class PortfolioServiceTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<InvaiseDbContext> _dbContextMock;
    private readonly PortfolioService _portfolioService;

    public PortfolioServiceTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _dbContextMock = new Mock<InvaiseDbContext>(new DbContextOptions<InvaiseDbContext>());
        _portfolioService = new PortfolioService(_dbServiceMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task GeneratePortfolioPerformancePdfAsync_UserNotFound_ThrowsArgumentException()
    {
        // Arrange
        string userId = "nonexistent";
        string portfolioId = "portfolio1";
        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;

        _dbServiceMock.Setup(db => db.GetUserByIdAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((User)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _portfolioService.GeneratePortfolioPerformancePdfAsync(userId, portfolioId, startDate, endDate));

        Assert.Equal($"User not found: {userId}", exception.Message);
    }

    [Fact]
    public async Task GeneratePortfolioPerformancePdfAsync_PortfolioNotFound_ThrowsArgumentException()
    {
        // Arrange
        string userId = "user1";
        string portfolioId = "nonexistent";
        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;

        var user = new User
        {
            Id = userId,
            DisplayName = "Test User",
            Email = "test@example.com"
        };

        _dbServiceMock.Setup(db => db.GetUserByIdAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(user);

        _dbServiceMock.Setup(db => db.GetPortfolioByIdWithPortfolioStocksAsync(It.IsAny<string>()))
            .ReturnsAsync((Portfolio)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _portfolioService.GeneratePortfolioPerformancePdfAsync(userId, portfolioId, startDate, endDate));

        Assert.Equal($"Portfolio not found: {portfolioId}", exception.Message);
    }
} 