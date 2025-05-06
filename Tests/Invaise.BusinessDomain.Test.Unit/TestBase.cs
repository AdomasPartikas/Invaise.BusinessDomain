namespace Invaise.BusinessDomain.Test.Unit;

public abstract class TestBase
{
    protected readonly Fixture _fixture;
    protected readonly IFixture _autoFixture;
    protected readonly Mock<ILogger> _loggerMock;
    protected readonly Mock<IConfiguration> _configurationMock;

    public TestBase()
    {
        _fixture = new Fixture();
        _autoFixture = new Fixture().Customize(new AutoMoqCustomization());
        _loggerMock = new Mock<ILogger>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Configure common IConfiguration behavior
        _configurationMock.Setup(x => x["JwtSettings:TokenKey"]).Returns("YourSecureTestKeyWith32Characters!!");
        _configurationMock.Setup(x => x["JwtSettings:TokenExpiryInDays"]).Returns("7");
    }
    
    // Helper to create a mock DbContext
    protected Mock<T> CreateMockDbContext<T>() where T : DbContext
    {
        return new Mock<T>();
    }
    
    // Helper to create a mock logger
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
} 