using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class ServiceAccountControllerTests : TestBase
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly ServiceAccountController _controller;

    public ServiceAccountControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new ServiceAccountController(_authServiceMock.Object);

        // Setup controller context
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task CreateServiceAccount_ValidModel_ReturnsOkWithAccount()
    {
        // Arrange
        var model = new CreateServiceAccountDto
        {
            Name = "test-service",
            Permissions = new string[] { "read:data", "write:data" }
        };

        var serviceAccount = new ServiceAccountDto
        {
            Id = "service-id",
            Name = "test-service",
            KeyUnhashed = "test-api-key",
            Permissions = new string[] { "read:data", "write:data" }
        };

        _authServiceMock.Setup(s => s.ServiceRegisterAsync(model.Name, model.Permissions))
            .ReturnsAsync(serviceAccount);

        // Act
        var result = await _controller.CreateServiceAccount(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAccount = Assert.IsType<ServiceAccountDto>(okResult.Value);
        Assert.Equal(serviceAccount.Id, returnedAccount.Id);
        Assert.Equal(serviceAccount.Name, returnedAccount.Name);
        Assert.Equal(serviceAccount.KeyUnhashed, returnedAccount.KeyUnhashed);
        Assert.Equal(serviceAccount.Permissions.Length, returnedAccount.Permissions.Length);
    }

    [Fact]
    public async Task CreateServiceAccount_AuthServiceThrowsException_ThrowsException()
    {
        // Arrange
        var model = new CreateServiceAccountDto
        {
            Name = "test-service",
            Permissions = new string[] { "read:data", "write:data" }
        };

        _authServiceMock.Setup(s => s.ServiceRegisterAsync(model.Name, model.Permissions))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.CreateServiceAccount(model));
    }
} 