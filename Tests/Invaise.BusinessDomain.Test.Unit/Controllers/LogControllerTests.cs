using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class LogControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly LogController _controller;

    public LogControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _controller = new LogController(_dbServiceMock.Object);
        
        // Setup controller context
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetLatestLogs_ServiceAccount_ReturnsOkWithLogs()
    {
        // Arrange
        int count = 10;
        var expectedLogs = new List<Log>
        {
            new Log { Id = 1, Message = "Log 1" },
            new Log { Id = 2, Message = "Log 2" }
        };
        
        _dbServiceMock.Setup(s => s.GetLatestLogsAsync(count))
            .ReturnsAsync(expectedLogs);
            
        // Set service account in HttpContext
        _controller.HttpContext.Items["ServiceAccount"] = new { Id = "service-account-id" };

        // Act
        var result = await _controller.GetLatestLogs(count);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLogs = Assert.IsAssignableFrom<IEnumerable<Log>>(okResult.Value);
        Assert.Equal(2, returnedLogs.Count());
    }

    [Fact]
    public async Task GetLatestLogs_AdminUser_ReturnsOkWithLogs()
    {
        // Arrange
        int count = 10;
        var adminUser = new User { Id = "admin-id", Role = "Admin" };
        var expectedLogs = new List<Log>
        {
            new Log { Id = 1, Message = "Log 1" },
            new Log { Id = 2, Message = "Log 2" }
        };
        
        _dbServiceMock.Setup(s => s.GetLatestLogsAsync(count))
            .ReturnsAsync(expectedLogs);
            
        // Set admin user in HttpContext
        _controller.HttpContext.Items["User"] = adminUser;

        // Act
        var result = await _controller.GetLatestLogs(count);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLogs = Assert.IsAssignableFrom<IEnumerable<Log>>(okResult.Value);
        Assert.Equal(2, returnedLogs.Count());
    }

    [Fact]
    public async Task GetLatestLogs_NonAdminUser_ReturnsForbid()
    {
        // Arrange
        int count = 10;
        var nonAdminUser = new User { Id = "user-id", Role = "User" };
        var logs = new List<Log>
        {
            new Log { Id = 1, Message = "Log 1" },
            new Log { Id = 2, Message = "Log 2" }
        };
        
        _dbServiceMock.Setup(s => s.GetLatestLogsAsync(count))
            .ReturnsAsync(logs);
            
        // Set non-admin user in HttpContext
        _controller.HttpContext.Items["User"] = nonAdminUser;

        // Act
        var result = await _controller.GetLatestLogs(count);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetLatestLogs_NoUserOrServiceAccount_ReturnsUnauthorized()
    {
        // Arrange
        int count = 10;
        
        // HttpContext does not have User or ServiceAccount

        // Act
        var result = await _controller.GetLatestLogs(count);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("Unauthorized", unauthorizedResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetLatestLogs_DatabaseServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        int count = 10;
        var adminUser = new User { Id = "admin-id", Role = "Admin" };
        
        _dbServiceMock.Setup(s => s.GetLatestLogsAsync(count))
            .ThrowsAsync(new Exception("Test exception"));
            
        // Set admin user in HttpContext
        _controller.HttpContext.Items["User"] = adminUser;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetLatestLogs(count));
    }
} 