using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Middleware;

public class JwtMiddlewareTests : TestBase
{
    private readonly RequestDelegate _nextMock;
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly IConfiguration _configuration;
    private readonly string _validKey = "YourSecureTestKeyWith32Characters!!";
    private readonly string _validIssuer = "test-issuer";
    private readonly string _validAudience = "test-audience";
    private readonly JwtMiddleware _middleware;
    
    public JwtMiddlewareTests()
    {
        _nextMock = (HttpContext context) => Task.CompletedTask;
        _dbServiceMock = new Mock<IDatabaseService>();
        
        // Setup configuration with required JWT settings
        var inMemorySettings = new Dictionary<string, string?> 
        {
            {"JWT:Key", _validKey},
            {"JWT:Issuer", _validIssuer},
            {"JWT:Audience", _validAudience}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        _middleware = new JwtMiddleware(_nextMock, _configuration);
    }

    private string GenerateJwtToken(string userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_validKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            {
                new Claim(JwtRegisteredClaimNames.NameId, userId),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = _validIssuer,
            Audience = _validAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task Invoke_NoAuthorizationHeader_DoesNotAttachUser()
    {
        // Arrange
        var context = new DefaultHttpContext();
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_ValidUserToken_AttachesUserToContext()
    {
        // Arrange
        var userId = "user123";
        var user = new User { Id = userId, DisplayName = "Test User" };
        var token = GenerateJwtToken(userId, "User");
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        
        _dbServiceMock.Setup(db => db.GetUserByIdAsync(userId, false))
            .ReturnsAsync(user);
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Same(user, context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_ValidServiceAccountToken_AttachesServiceAccountToContext()
    {
        // Arrange
        var serviceAccountId = "service123";
        var serviceAccount = new ServiceAccount { Id = serviceAccountId, Name = "Test Service" };
        var token = GenerateJwtToken(serviceAccountId, "Service");
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        
        _dbServiceMock.Setup(db => db.GetServiceAccountAsync(serviceAccountId))
            .ReturnsAsync(serviceAccount);
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Same(serviceAccount, context.Items["ServiceAccount"]);
        Assert.Null(context.Items["User"]);
    }

    [Fact]
    public async Task Invoke_InvalidToken_DoesNotAttachUser()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer invalidtoken";
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_ExpiredToken_DoesNotAttachUser()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_validKey);
        
        // Create a token that is already expired
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, "user123"),
                new Claim(ClaimTypes.Role, "User")
            }),
            NotBefore = now.AddMinutes(-10),  // Token is valid from 10 minutes ago
            Expires = now.AddMinutes(-5),     // Token expired 5 minutes ago
            Issuer = _validIssuer,
            Audience = _validAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var expiredToken = tokenHandler.WriteToken(token);
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {expiredToken}";
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_TokenWithWrongIssuer_DoesNotAttachUser()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_validKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, "user123"),
                new Claim(ClaimTypes.Role, "User")
            }),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = "wrong-issuer", // Wrong issuer
            Audience = _validAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var invalidToken = tokenHandler.WriteToken(token);
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {invalidToken}";
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_MissingJwtConfiguration_DoesNotAttachUser()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();
        var middlewareWithEmptyConfig = new JwtMiddleware(_nextMock, emptyConfig);
        
        var userId = "user123";
        var token = GenerateJwtToken(userId, "User");
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        
        // Act
        await middlewareWithEmptyConfig.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_UserNotFound_DoesNotAttachUser()
    {
        // Arrange
        var userId = "nonexistentUser";
        var token = GenerateJwtToken(userId, "User");
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        
        _dbServiceMock.Setup(db => db.GetUserByIdAsync(userId, false))
            .ReturnsAsync((User?)null);
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }

    [Fact]
    public async Task Invoke_ServiceAccountNotFound_DoesNotAttachServiceAccount()
    {
        // Arrange
        var serviceAccountId = "nonexistentService";
        var token = GenerateJwtToken(serviceAccountId, "Service");
        
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        
        _dbServiceMock.Setup(db => db.GetServiceAccountAsync(serviceAccountId))
            .ReturnsAsync((ServiceAccount?)null);
        
        // Act
        await _middleware.Invoke(context, _dbServiceMock.Object);
        
        // Assert
        Assert.Null(context.Items["User"]);
        Assert.Null(context.Items["ServiceAccount"]);
    }
} 