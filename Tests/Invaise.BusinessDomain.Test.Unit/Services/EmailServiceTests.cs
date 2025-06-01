using Invaise.BusinessDomain.API.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;

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
} 