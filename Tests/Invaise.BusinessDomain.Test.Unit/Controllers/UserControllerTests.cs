namespace Invaise.BusinessDomain.Test.Unit.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Invaise.BusinessDomain.Test.Unit.Utilities;

public class UserControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserController _controller;
    private readonly User _testUser;

    public UserControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _mapperMock = new Mock<IMapper>();
        _controller = new UserController(_dbServiceMock.Object, _mapperMock.Object);
        
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
            { "User", _testUser }
        };
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public void GetCurrentUser_ReturnsCurrentUser()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = _testUser.Id,
            Name = _testUser.DisplayName,
            Email = "test@example.com",
            Role = _testUser.Role
        };
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);
        
        // Act
        var result = _controller.GetCurrentUser();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userDto, okResult.Value);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsOkResult()
    {
        // Arrange
        var userId = "user1";
        var user = new User
        {
            Id = userId,
            DisplayName = "Test User",
            Role = "User"
        };
        
        var userDto = new UserDto
        {
            Id = userId,
            Name = "Test User",
            Role = "User"
        };
        
        _dbServiceMock.Setup(db => db.GetUserByIdAsync(userId, true))
            .ReturnsAsync(user);
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);
        
        // Act
        var result = await _controller.GetUserById(userId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userDto, okResult.Value);
    }

    [Fact]
    public async Task GetUserById_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent";
        
        _dbServiceMock.Setup(db => db.GetUserByIdAsync(userId, true))
            .ReturnsAsync((User)null);
        
        // Act
        var result = await _controller.GetUserById(userId);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "user1", DisplayName = "User One", Role = "User" },
            new User { Id = "user2", DisplayName = "User Two", Role = "Admin" }
        };
        
        _dbServiceMock.Setup(db => db.GetAllUsersAsync(false))
            .ReturnsAsync(users);
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns<User>(u => new UserDto { Id = u.Id, Name = u.DisplayName, Role = u.Role });
        
        // Act
        var result = await _controller.GetAllUsers();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultUsers = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
        Assert.Equal(2, resultUsers.Count());
    }

    [Fact]
    public async Task UpdateUserActiveStatus_ValidId_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = "user1";
        var isActive = false;
        
        var updatedUser = new User
        {
            Id = userId,
            DisplayName = "Test User",
            Role = "User",
            IsActive = isActive
        };
        
        var userDto = new UserDto
        {
            Id = userId,
            Name = "Test User",
            Role = "User",
            IsActive = isActive
        };
        
        _dbServiceMock.Setup(db => db.UpdateUserActiveStatusAsync(userId, isActive))
            .ReturnsAsync(updatedUser);
        
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);
        
        // Act
        var result = await _controller.UpdateUserActiveStatus(userId, isActive);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userDto, okResult.Value);
    }

    [Fact]
    public async Task UpdateUserActiveStatus_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent";
        var isActive = false;
        
        _dbServiceMock.Setup(db => db.UpdateUserActiveStatusAsync(userId, isActive))
            .ThrowsAsync(new InvalidOperationException("User not found"));
        
        // Act
        var result = await _controller.UpdateUserActiveStatus(userId, isActive);
        
        // Assert
        TestFactory.AssertNotFoundResult(result);
    }

    [Fact]
    public async Task UpdateUserActiveStatus_Exception_ReturnsInternalServerError()
    {
        // Arrange
        var userId = "user1";
        var isActive = false;
        
        _dbServiceMock.Setup(db => db.UpdateUserActiveStatusAsync(userId, isActive))
            .ThrowsAsync(new Exception("Database error"));
        
        // Act
        var result = await _controller.UpdateUserActiveStatus(userId, isActive);
        
        // Assert
        TestFactory.AssertInternalServerErrorResult(result);
    }

    [Fact]
    public async Task IsUserAdmin_AdminUser_ReturnsOkResult()
    {
        // Arrange
        var adminUser = new User
        {
            Id = "admin1",
            DisplayName = "Admin User",
            Role = "Admin"
        };
        
        var httpContext = new DefaultHttpContext();
        httpContext.Items = new Dictionary<object, object?>
        {
            { "User", adminUser }
        };
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Act
        var result = await _controller.IsUserAdmin();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        TestFactory.AssertResponseMessage(result, "User is an admin");
    }

    [Fact]
    public async Task IsUserAdmin_NonAdminUser_ReturnsForbid()
    {
        // Act
        var result = await _controller.IsUserAdmin();
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdatePersonalInfo_OwnInfo_ReturnsUpdatedInfo()
    {
        // Arrange
        var userId = "user1";
        var model = new UserPersonalInfoModel
        {
            Address = "123 Main St",
            PhoneNumber = "555-1234",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        
        var existingUser = new User
        {
            Id = userId,
            DisplayName = "Test User",
            PersonalInfo = new UserPersonalInfo
            {
                UserId = userId,
                Address = "Old Address",
                PhoneNumber = "Old Phone"
            }
        };
        
        var updatedInfo = new UserPersonalInfo
        {
            UserId = userId,
            Address = model.Address,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth.Value
        };
        
        var infoDto = new UserPersonalInfoDto
        {
            Address = model.Address,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth.Value
        };
        
        _dbServiceMock.Setup(db => db.GetUserByIdAsync(userId, It.IsAny<bool>()))
            .ReturnsAsync(existingUser);
        
        _dbServiceMock.Setup(db => db.UpdateUserPersonalInfoAsync(userId, It.IsAny<UserPersonalInfo>()))
            .ReturnsAsync(updatedInfo);
        
        _mapperMock.Setup(m => m.Map<UserPersonalInfoDto>(It.IsAny<UserPersonalInfo>()))
            .Returns(infoDto);
        
        // Act
        var result = await _controller.UpdatePersonalInfo(userId, model);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(infoDto, okResult.Value);
    }

    [Fact]
    public async Task UpdatePersonalInfo_OtherUserInfo_NonAdmin_ReturnsForbid()
    {
        // Arrange
        var userId = "user2"; // Different from the current user
        var model = new UserPersonalInfoModel
        {
            Address = "123 Main St",
            PhoneNumber = "555-1234"
        };
        
        // Act
        var result = await _controller.UpdatePersonalInfo(userId, model);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdatePreferences_OwnPreferences_ReturnsUpdatedPreferences()
    {
        // Arrange
        var userId = "user1";
        var preferences = new UserPreferencesDto
        {
            RiskTolerance = 3,
            InvestmentHorizon = "5"
        };
        
        var updatedPreferences = new UserPreferences
        {
            RiskTolerance = preferences.RiskTolerance,
            InvestmentHorizon = preferences.InvestmentHorizon
        };
        
        _dbServiceMock.Setup(db => db.UpdateUserPreferencesAsync(userId, It.IsAny<UserPreferences>()))
            .ReturnsAsync(updatedPreferences);
        
        _mapperMock.Setup(m => m.Map<UserPreferencesDto>(It.IsAny<UserPreferences>()))
            .Returns(preferences);
        
        // Act
        var result = await _controller.UpdatePreferences(userId, preferences);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(preferences, okResult.Value);
    }

    [Fact]
    public async Task UpdatePreferences_OtherUserPreferences_NonAdmin_ReturnsForbid()
    {
        // Arrange
        var userId = "user2"; // Different from the current user
        var preferences = new UserPreferencesDto
        {
            RiskTolerance = 3,
            InvestmentHorizon = "5"
        };
        
        // Act
        var result = await _controller.UpdatePreferences(userId, preferences);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }
} 