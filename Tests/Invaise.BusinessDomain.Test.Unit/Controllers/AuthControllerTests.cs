using Invaise.BusinessDomain.Test.Unit.Utilities;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class AuthControllerTests : TestBase
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_ValidModel_ReturnsOkResult()
    {
        // Arrange
        var registerModel = _fixture.Create<RegisterModel>();
        var authResponse = _fixture.Create<AuthResponse>();
        
        _authServiceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterModel>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(registerModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(authResponse);
        _authServiceMock.Verify(s => s.RegisterAsync(It.IsAny<RegisterModel>()), Times.Once);
    }

    [Fact]
    public async Task Register_ThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var registerModel = _fixture.Create<RegisterModel>();
        var errorMessage = "Email already exists";
        
        _authServiceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterModel>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Act
        var result = await _controller.Register(registerModel);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        TestFactory.AssertResponseMessage(result, errorMessage);
    }

    [Fact]
    public async Task Register_ThrowsGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var registerModel = _fixture.Create<RegisterModel>();
        
        _authServiceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterModel>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Register(registerModel);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var loginModel = _fixture.Create<LoginModel>();
        var authResponse = _fixture.Create<AuthResponse>();
        
        _authServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginModel>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(loginModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginModel = _fixture.Create<LoginModel>();
        var errorMessage = "Invalid email or password";
        
        _authServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginModel>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Act
        var result = await _controller.Login(loginModel);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        TestFactory.AssertResponseMessage(result, errorMessage);
    }

    [Fact]
    public async Task RefreshToken_ValidModel_ReturnsOkResult()
    {
        // Arrange
        var refreshModel = _fixture.Create<RefreshModel>();
        var authResponse = _fixture.Create<AuthResponse>();
        
        _authServiceMock.Setup(s => s.RefreshToken(It.IsAny<RefreshModel>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.RefreshToken(refreshModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task ServiceLogin_ValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var serviceLoginModel = _fixture.Create<ServiceLoginModel>();
        var authResponse = _fixture.Create<AuthResponse>();
        
        _authServiceMock.Setup(s => s.ServiceLoginAsync(It.IsAny<ServiceLoginModel>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.ServiceLogin(serviceLoginModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOkResult()
    {
        // Arrange
        var forgotPasswordModel = new ForgotPasswordModel { Email = "test@example.com" };
        
        _authServiceMock.Setup(s => s.ForgotPasswordAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ForgotPassword(forgotPasswordModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        TestFactory.AssertResponseMessage(result, "If your email exists in our system, a password reset email has been sent.");
    }
} 