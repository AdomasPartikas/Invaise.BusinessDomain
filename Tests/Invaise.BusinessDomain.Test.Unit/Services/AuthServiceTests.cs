namespace Invaise.BusinessDomain.Test.Unit.Services;

public class AuthServiceTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _mapperMock = new Mock<IMapper>();
        _emailServiceMock = new Mock<IEmailService>();
        
        // Use the configuration from TestBase and add additional JWT settings
        _configurationMock.Setup(c => c["JWT:Key"]).Returns("your-secret-key-here-must-be-at-least-32-characters-long");
        _configurationMock.Setup(c => c["JWT:Issuer"]).Returns("your-issuer");
        _configurationMock.Setup(c => c["JWT:Audience"]).Returns("your-audience");
        _configurationMock.Setup(c => c["JWT:ExpiryInMinutes"]).Returns("60");

        _configuration = _configurationMock.Object;
        
        _authService = new AuthService(_dbServiceMock.Object, _configuration, _mapperMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsAuthResponse()
    {
        // Arrange
        var registerModel = new RegisterModel
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        };
        
        string hashedEmail = "hashed_email";
        
        _dbServiceMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        
        _dbServiceMock.Setup(db => db.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(new User
            {
                Id = "user1",
                DisplayName = registerModel.Name,
                Email = hashedEmail,
                Role = "User"
            });
        
        var userDto = new UserDto
        {
            Id = "user1",
            Name = registerModel.Name,
            Email = registerModel.Email,
            Role = "User"
        };
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);
        
        _emailServiceMock.Setup(e => e.SendRegistrationConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await _authService.RegisterAsync(registerModel);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEqual(default, result.ExpiresAt);
        Assert.Equal(userDto, result.User);
        
        _dbServiceMock.Verify(db => db.CreateUserAsync(It.IsAny<User>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendRegistrationConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var registerModel = new RegisterModel
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "Password123!"
        };
        
        _dbServiceMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Email = "hashed_existing@example.com" }); // Existing user
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(registerModel));
        
        _dbServiceMock.Verify(db => db.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var loginModel = new LoginModel
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        
        var user = new User
        {
            Id = "user1",
            DisplayName = "Test User",
            Email = "hashed_email",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "User",
            IsActive = true
        };
        
        _dbServiceMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        _dbServiceMock.Setup(db => db.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(user);
        
        var userDto = new UserDto
        {
            Id = "user1",
            Name = "Test User",
            Email = loginModel.Email,
            Role = "User"
        };
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);
        
        // Act
        var result = await _authService.LoginAsync(loginModel);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEqual(default, result.ExpiresAt);
        Assert.Equal(userDto, result.User);
        
        _dbServiceMock.Verify(db => db.UpdateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var loginModel = new LoginModel
        {
            Email = "inactive@example.com",
            Password = "Password123!"
        };
        
        var user = new User
        {
            Id = "user2",
            DisplayName = "Inactive User",
            Email = "hashed_email",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "User",
            IsActive = false
        };
        
        _dbServiceMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(loginModel));
        Assert.Equal("This account is inactive", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var loginModel = new LoginModel
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };
        
        var user = new User
        {
            Id = "user1",
            DisplayName = "Test User",
            Email = "hashed_email",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "User",
            IsActive = true
        };
        
        _dbServiceMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(loginModel));
        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task ServiceLoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var serviceLoginModel = new ServiceLoginModel
        {
            Id = "service1",
            Key = "service-key"
        };
        
        var serviceAccount = new ServiceAccount
        {
            Id = "service1",
            Name = "Test Service",
            Key = BCrypt.Net.BCrypt.HashPassword("service-key"),
            Role = "Service"
        };
        
        _dbServiceMock.Setup(db => db.GetServiceAccountAsync(It.IsAny<string>()))
            .ReturnsAsync(serviceAccount);
        
        var serviceDto = new UserDto
        {
            Id = "service1",
            Name = "Test Service",
            Role = "Service"
        };
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<ServiceAccount>()))
            .Returns(serviceDto);
        
        // Act
        var result = await _authService.ServiceLoginAsync(serviceLoginModel);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEqual(default, result.ExpiresAt);
        Assert.Equal(serviceDto, result.User);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ValidEmail_ReturnsTrueAndSendsEmail()
    {
        // Arrange
        string email = "test@example.com";
        string hashedEmail = "hashed_email";
        
        var user = new User
        {
            Id = "user1",
            DisplayName = "Test User",
            Email = hashedEmail,
            IsActive = true
        };
        
        _dbServiceMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        _dbServiceMock.Setup(db => db.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(user);
        
        _emailServiceMock.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await _authService.ForgotPasswordAsync(email);
        
        // Assert
        Assert.True(result);
        _emailServiceMock.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
} 