using Invaise.BusinessDomain.API.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class EmailServiceTests : TestBase
{
    private readonly EmailService _service;
    private readonly Mock<IConfiguration> _configMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;

    public EmailServiceTests()
    {
        // Setup configuration mock
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<Serilog.ILogger>();
        
        // Setup configuration mock values
        _configMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        _configMock.Setup(x => x["Email:Port"]).Returns("587");
        _configMock.Setup(x => x["Email:UseSsl"]).Returns("true");
        _configMock.Setup(x => x["Email:Username"]).Returns("user@example.com");
        _configMock.Setup(x => x["Email:Password"]).Returns("password123");
        _configMock.Setup(x => x["Email:SenderEmail"]).Returns("noreply@invaise.com");
        _configMock.Setup(x => x["Email:SenderName"]).Returns("Invaise");
        _configMock.Setup(x => x["Email:UseAuthentication"]).Returns("true");
        
        // Create service with mocks
        _service = new EmailService(_configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_InitializesWithValidConfiguration()
    {
        // Act & Assert
        var service = new EmailService(_configMock.Object, _loggerMock.Object);
        // If no exception is thrown, the test passes
        Assert.NotNull(service);
    }
    
    [Fact]
    public void Constructor_ThrowsException_WhenSmtpServerNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns((string)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }
    
    [Fact]
    public void Constructor_ThrowsException_WhenPortNotConfigured()
    {
        // Arrange
        var invalidConfigMock = new Mock<IConfiguration>();
        invalidConfigMock.Setup(x => x["Email:SmtpServer"]).Returns("smtp.example.com");
        invalidConfigMock.Setup(x => x["Email:Port"]).Returns((string)null);
        
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
        invalidConfigMock.Setup(x => x["Email:UseSsl"]).Returns((string)null);
        
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
        invalidConfigMock.Setup(x => x["Email:Username"]).Returns((string)null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(invalidConfigMock.Object, _loggerMock.Object));
    }
    
    // Note: In a real test scenario, we would mock MailKit's SmtpClient to verify that 
    // the correct emails are being sent. However, since the service directly creates a new
    // SmtpClient instance internally, we would need to use a library like TypeMock or
    // refactor the code to accept an ISmtpClient interface for better testability.
    // For this example, we're just testing the configuration validation.
} 