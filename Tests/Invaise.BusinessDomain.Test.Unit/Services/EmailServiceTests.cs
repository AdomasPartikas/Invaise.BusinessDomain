using Invaise.BusinessDomain.API.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Net.Sockets;
using MimeKit;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class EmailServiceTests : TestBase
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;

    public EmailServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<Serilog.ILogger>();
        
        _configMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        _configMock.Setup(x => x["Email:Port"]).Returns("587");
        _configMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        _configMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        _configMock.Setup(x => x["Email:Password"]).Returns("password123");
        _configMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        _configMock.Setup(x => x["Email:SenderName"]).Returns("Invaise");
        _configMock.Setup(x => x["Email:UseAuthentication"]).Returns("true");
    }

    [Fact]
    public void Constructor_InitializesWithValidConfiguration()
    {
        // Act & Assert
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        Assert.NotNull(service);
    }
    
    [Fact]
    public void Constructor_ThrowsException_WhenSmtpServerNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }
    
    [Fact]
    public void Constructor_ThrowsException_WhenPortNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }
    
    [Fact]
    public void Constructor_ThrowsException_WhenUseSslNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }
    
    [Fact]
    public void Constructor_ThrowsException_WhenUsernameNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenPasswordNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        invalidConfigMock.Setup(x => x["Email:Password"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenSenderEmailNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        invalidConfigMock.Setup(x => x["Email:Password"]).Returns("password123");
        invalidConfigMock.Setup(x => x["Email:SenderEmail"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenSenderNameNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        invalidConfigMock.Setup(x => x["Email:Password"]).Returns("password123");
        invalidConfigMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        invalidConfigMock.Setup(x => x["Email:SenderName"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenUseAuthenticationNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        invalidConfigMock.Setup(x => x["Email:Password"]).Returns("password123");
        invalidConfigMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        invalidConfigMock.Setup(x => x["Email:SenderName"]).Returns("Invaise");
        invalidConfigMock.Setup(x => x["Email:UseAuthentication"]).Returns((string?)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenPortIsNotInteger()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("not_a_number");
        
        // Act & Assert
        Assert.Throws<FormatException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenUseSslIsNotBoolean()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("not_a_boolean");
        
        // Act & Assert
        Assert.Throws<FormatException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenUseAuthenticationIsNotBoolean()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns("587");
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        invalidConfigMock.Setup(x => x["Email:Password"]).Returns("password123");
        invalidConfigMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        invalidConfigMock.Setup(x => x["Email:SenderName"]).Returns("Invaise");
        invalidConfigMock.Setup(x => x["Email:UseAuthentication"]).Returns("not_a_boolean");
        
        // Act & Assert
        Assert.Throws<FormatException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void EmailService_ImplementsIEmailService()
    {
        // Act
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Assert
        Assert.IsAssignableFrom<IEmailService>(service);
    }

    [Fact]
    public async Task SendRegistrationConfirmationEmailAsync_ThrowsException_WhenSmtpFails()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(async () => 
            await service.SendRegistrationConfirmationEmailAsync("test@example.com", "TestUser"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ThrowsException_WhenSmtpFails()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(async () => 
            await service.SendPasswordResetEmailAsync("test@example.com", "TestUser", "tempPass123"));
    }

    [Fact]
    public async Task SendRegistrationConfirmationEmailAsync_WithNullEmail_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await service.SendRegistrationConfirmationEmailAsync(null!, "TestUser"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullEmail_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await service.SendPasswordResetEmailAsync(null!, "TestUser", "tempPass123"));
    }

    [Fact]
    public async Task SendRegistrationConfirmationEmailAsync_WithEmptyEmail_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<ParseException>(async () => 
            await service.SendRegistrationConfirmationEmailAsync("", "TestUser"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithEmptyEmail_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<ParseException>(async () => 
            await service.SendPasswordResetEmailAsync("", "TestUser", "tempPass123"));
    }

    [Fact]
    public async Task SendRegistrationConfirmationEmailAsync_WithNullUsername_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(async () => 
            await service.SendRegistrationConfirmationEmailAsync("test@example.com", null!));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullUsername_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(async () => 
            await service.SendPasswordResetEmailAsync("test@example.com", null!, "tempPass123"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullTemporaryPassword_ThrowsException()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(async () => 
            await service.SendPasswordResetEmailAsync("test@example.com", "TestUser", null!));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    public void Constructor_WorksWithFalseAuthentication(string authValue)
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        configMock.Setup(x => x["Email:Port"]).Returns("587");
        configMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        configMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        configMock.Setup(x => x["Email:Password"]).Returns("password123");
        configMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        configMock.Setup(x => x["Email:SenderName"]).Returns("Invaise");
        configMock.Setup(x => x["Email:UseAuthentication"]).Returns(authValue);
        
        // Act & Assert
        var service = new EmailService(configMock.Object, _loggerMock.Object);
        Assert.NotNull(service);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    public void Constructor_WorksWithFalseSsl(string sslValue)
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        configMock.Setup(x => x["Email:Port"]).Returns("587");
        configMock.Setup(x => x["Email:UseSsl"]).Returns(sslValue);
        configMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        configMock.Setup(x => x["Email:Password"]).Returns("password123");
        configMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        configMock.Setup(x => x["Email:SenderName"]).Returns("Invaise");
        configMock.Setup(x => x["Email:UseAuthentication"]).Returns("true");
        
        // Act & Assert
        var service = new EmailService(configMock.Object, _loggerMock.Object);
        Assert.NotNull(service);
    }
} 