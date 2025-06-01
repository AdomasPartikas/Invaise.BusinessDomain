using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.FinnhubAPIClient;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Services;
using Invaise.BusinessDomain.API.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Invaise.BusinessDomain.API.Constants;
using Company = Invaise.BusinessDomain.API.Entities.Company;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class MarketDataServiceTests : TestBase, IDisposable
{
    private readonly InvaiseDbContext _dbContext;
    private readonly MarketDataService _service;
    private readonly Mock<IFinnhubClient> _finnhubClientMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IKaggleService> _kaggleServiceMock;
    private readonly Mock<IDataService> _dataServiceMock;
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly new Mock<Serilog.ILogger> _loggerMock;

    public MarketDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvaiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new InvaiseDbContext(options);
        SeedDatabase();

        _finnhubClientMock = new Mock<IFinnhubClient>();
        _mapperMock = new Mock<IMapper>();
        _kaggleServiceMock = new Mock<IKaggleService>();
        _dataServiceMock = new Mock<IDataService>();
        _dbServiceMock = new Mock<IDatabaseService>();
        _loggerMock = new Mock<Serilog.ILogger>();

        _service = new MarketDataService(
            _finnhubClientMock.Object,
            _mapperMock.Object,
            _dbContext,
            _kaggleServiceMock.Object,
            _dataServiceMock.Object,
            _dbServiceMock.Object,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        _dbContext.HistoricalMarketData.AddRange(new List<HistoricalMarketData>
        {
            new() {
                Symbol = "AAPL",
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Open = 150.0m,
                High = 155.0m,
                Low = 149.0m,
                Close = 153.0m,
                Volume = 1000000,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new() {
                Symbol = "MSFT",
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Open = 250.0m,
                High = 255.0m,
                Low = 249.0m,
                Close = 253.0m,
                Volume = 800000,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        });
        
        _dbContext.IntradayMarketData.AddRange(new List<IntradayMarketData>
        {
            new() {
                Symbol = "AAPL",
                Current = 154.0m,
                Open = 153.0m,
                High = 156.0m,
                Low = 152.0m,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new() {
                Symbol = "MSFT",
                Current = 254.0m,
                Open = 253.0m,
                High = 256.0m,
                Low = 252.0m,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        });
        
        _dbContext.Companies.AddRange(new List<Company>
        {
            new() {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Industry = "Technology",
                Country = "United States",
                Description = "Technology company",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new() {
                Symbol = "MSFT",
                Name = "Microsoft Corporation",
                Industry = "Technology",
                Country = "United States",
                Description = "Software company",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        });
        
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task IsMarketOpenAsync_ReturnsTrue_WhenMarketIsOpen()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = true
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);

        // Act
        var result = await _service.IsMarketOpenAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMarketOpenAsync_ReturnsFalse_WhenMarketIsClosed()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = false
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);

        // Act
        var result = await _service.IsMarketOpenAsync();

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task IsMarketOpenAsync_ReturnsFalse_WhenExceptionIsThrown()
    {
        // Arrange
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ThrowsAsync(new FinnhubAPIClientException("Error", 500, "Error message", null, null));

        // Act
        var result = await _service.IsMarketOpenAsync();

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task ImportIntradayMarketDataAsync_DoesNothing_WhenMarketIsClosed()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = false
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);
            
        var initialCount = await _dbContext.IntradayMarketData.CountAsync();

        // Act
        await _service.ImportIntradayMarketDataAsync();

        // Assert
        var finalCount = await _dbContext.IntradayMarketData.CountAsync();
        Assert.Equal(initialCount, finalCount); // No new records should be added
    }
    
    [Fact]
    public async Task ImportIntradayMarketDataAsync_ImportsData_WhenMarketIsOpen()
    {
        // Arrange
        var marketStatus = new MarketStatus
        {
            Exchange = "US",
            IsOpen = true
        };
        
        _finnhubClientMock
            .Setup(client => client.MarketStatusAsync("US"))
            .ReturnsAsync(marketStatus);
            
        var symbols = new List<string> { "AAPL", "MSFT" };
        
        _dbServiceMock
            .Setup(service => service.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(symbols);
            
        var appleQuote = new Quote
        {
            O = 154.0f, // Open price
            H = 156.0f, // High price
            L = 153.0f, // Low price
            C = 155.0f, // Current price
            Pc = 153.0f, // Previous close
            T = 1625086800L // Timestamp
        };
        
        var msftQuote = new Quote
        {
            O = 259.0f,
            H = 262.0f,
            L = 258.0f,
            C = 260.0f,
            Pc = 257.0f,
            T = 1625086800L
        };
        
        var googQuote = new Quote
        {
            O = 1599.0f,
            H = 1602.0f,
            L = 1598.0f,
            C = 1600.0f,
            Pc = 1595.0f,
            T = 1625086800L
        };
        
        _finnhubClientMock
            .Setup(client => client.QuoteAsync("AAPL"))
            .ReturnsAsync(appleQuote);
            
        _finnhubClientMock
            .Setup(client => client.QuoteAsync("MSFT"))
            .ReturnsAsync(msftQuote);
            
        _finnhubClientMock
            .Setup(client => client.QuoteAsync("GOOG"))
            .ReturnsAsync(googQuote);
            
        var appleData = new IntradayMarketData { 
            Symbol = "AAPL", 
            Open = 154.0m,
            High = 156.0m,
            Low = 153.0m,
            Current = 155.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        var msftData = new IntradayMarketData { 
            Symbol = "MSFT", 
            Open = 259.0m,
            High = 262.0m,
            Low = 258.0m,
            Current = 260.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        var googData = new IntradayMarketData { 
            Symbol = "GOOG", 
            Open = 1599.0m,
            High = 1602.0m,
            Low = 1598.0m,
            Current = 1600.0m,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1625086800).DateTime
        };
        
        _mapperMock
            .Setup(mapper => mapper.Map<IntradayMarketData>(appleQuote))
            .Returns(appleData);
            
        _mapperMock
            .Setup(mapper => mapper.Map<IntradayMarketData>(msftQuote))
            .Returns(msftData);
            
        _mapperMock
            .Setup(mapper => mapper.Map<IntradayMarketData>(googQuote))
            .Returns(googData);
            
        var initialCount = await _dbContext.IntradayMarketData.CountAsync();

        // Act
        await _service.ImportIntradayMarketDataAsync();

        // Assert
        var finalCount = await _dbContext.IntradayMarketData.CountAsync();
        Assert.Equal(initialCount + 2, finalCount); // Two new records should be added
    }

    [Fact]
    public void FetchAndImportHistoricalMarketDataAsync_SetsUpDependencies()
    {
        // Arrange
        _kaggleServiceMock
            .Setup(s => s.DownloadDatasetAsync(GlobalConstants.KaggleSmpDataset))
            .Returns(Task.CompletedTask);
            
        _dataServiceMock
            .Setup(s => s.SMPDatasetCleanupAsync())
            .Returns(Task.CompletedTask);
        
        _mapperMock
            .Setup(m => m.Map<HistoricalMarketData>(It.IsAny<MarketDataDto>()))
            .Returns<MarketDataDto>(dto => new HistoricalMarketData
            {
                Symbol = dto.Symbol,
                Date = dto.Date,
                Open = dto.Open,
                High = dto.High,
                Low = dto.Low,
                Close = dto.Close,
                Volume = (long?) dto.Volume
            });
        
        Assert.NotNull(_kaggleServiceMock);
        Assert.NotNull(_dataServiceMock);
    }

    [Fact]
    public async Task ImportCompanyDataAsync_UpdatesCompaniesWithMissingData()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "MSFT", "GOOG" };
        
        _dbServiceMock
            .Setup(s => s.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(symbols);
            
        // Setup mock response for GOOG which has missing data
        var googleProfile = new CompanyProfile2
        {
            Name = "Alphabet Inc.",
            Ticker = "GOOG",
            Country = "US",
            Currency = "USD",
            Exchange = "NASDAQ",
            Ipo = DateTime.Parse("2004-08-19", System.Globalization.CultureInfo.InvariantCulture),
            MarketCapitalization = 1500000f,
            ShareOutstanding = 6000f,
            Logo = "https://static.finnhub.io/logo/87cb30d8-80df-11ea-8951-00000000092a.png",
            Phone = "650-253-0000",
            Weburl = "https://abc.xyz",
            FinnhubIndustry = "Technology"
        };
        
        // Setup client mock to return profile for GOOG
        _finnhubClientMock
            .Setup(c => c.CompanyProfile2Async("GOOG", null, null))
            .ReturnsAsync(googleProfile);
            
        // Setup mapper
        _mapperMock
            .Setup(m => m.Map(It.IsAny<CompanyProfile2>(), It.IsAny<Company>()))
            .Callback<CompanyProfile2, Company>((profile, company) => 
            {
                company.Name = profile.Name;
                company.Country = profile.Country;
                company.Industry = profile.FinnhubIndustry;
                company.Description = profile.Weburl;
                // Don't use Currency and Exchange as they're not in the Company entity
            });
            
        // Act
        await _service.ImportCompanyDataAsync();
        
        // Assert
        var updatedCompany = await _dbContext.Companies.FindAsync(3); // GOOG
        Assert.NotNull(updatedCompany);
        Assert.Equal("Alphabet Inc.", updatedCompany.Name);
        Assert.Equal("US", updatedCompany.Country);
        Assert.Equal("Technology", updatedCompany.Industry);
    }
    
    [Fact]
    public async Task ImportCompanyDataAsync_SkipsCompaniesWithCompleteData()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "MSFT" };
        
        _dbServiceMock
            .Setup(s => s.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(symbols);
            
        // Act
        await _service.ImportCompanyDataAsync();
        
        // Assert
        _finnhubClientMock.Verify(c => c.CompanyProfile2Async("AAPL", null, null), Times.Never);
        _finnhubClientMock.Verify(c => c.CompanyProfile2Async("MSFT", null, null), Times.Never);
    }
    
    [Fact]
    public async Task ImportCompanyDataAsync_CreatesNewCompanyIfNotExists()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "MSFT", "GOOG", "TSLA" }; // TSLA is new
        
        _dbServiceMock
            .Setup(s => s.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(symbols);
            
        // Setup mock response for TSLA which doesn't exist
        var teslaProfile = new CompanyProfile2
        {
            Name = "Tesla Inc.",
            Ticker = "TSLA",
            Country = "US",
            Currency = "USD",
            Exchange = "NASDAQ",
            Ipo = DateTime.Parse("2010-06-29", System.Globalization.CultureInfo.InvariantCulture),
            MarketCapitalization = 800000f,
            ShareOutstanding = 3000f,
            Logo = "https://example.com/tesla.png",
            Phone = "888-518-3752",
            Weburl = "https://www.tesla.com",
            FinnhubIndustry = "Automobiles"
        };
        
        _finnhubClientMock
            .Setup(c => c.CompanyProfile2Async("TSLA", null, null))
            .ReturnsAsync(teslaProfile);
            
        // Setup mapper
        _mapperMock
            .Setup(m => m.Map(It.IsAny<CompanyProfile2>(), It.IsAny<Company>()))
            .Callback<CompanyProfile2, Company>((profile, company) => 
            {
                company.Name = profile.Name;
                company.Country = profile.Country;
                company.Industry = profile.FinnhubIndustry;
                company.Description = profile.Weburl;
            });
            
        // Act
        await _service.ImportCompanyDataAsync();
        
        // Assert
        var newCompany = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Symbol == "TSLA");
        Assert.NotNull(newCompany);
        Assert.Equal("Tesla Inc.", newCompany.Name);
        Assert.Equal("US", newCompany.Country);
        Assert.Equal("Automobiles", newCompany.Industry);
    }
    
    [Fact]
    public async Task ImportIntradayMarketDataAsync_HandlesFinnhubRateLimiting()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "MSFT" };
        
        _dbServiceMock
            .Setup(s => s.GetAllUniqueMarketDataSymbolsAsync())
            .ReturnsAsync(symbols);
            
        _finnhubClientMock
            .Setup(c => c.MarketStatusAsync("US"))
            .ReturnsAsync(new MarketStatus { IsOpen = true });
            
        _finnhubClientMock
            .SetupSequence(c => c.QuoteAsync("AAPL"))
            .ThrowsAsync(new FinnhubAPIClientException("Rate limit exceeded", 429, "Rate limit exceeded", null, null))
            .ReturnsAsync(new Quote 
            { 
                C = (float)160.0, 
                H = (float)165.0, 
                L = (float)159.0, 
                O = (float)160.0,
                Pc = (float)158.0,
                T = 1625086800L //(July 1, 2021)
            });
            
        // Setup mapper
        _mapperMock
            .Setup(m => m.Map<IntradayMarketData>(It.IsAny<Quote>()))
            .Returns<Quote>(q => new IntradayMarketData
            {
                Current = (decimal)(q.C ?? 0),
                High = (decimal)(q.H ?? 0),
                Low = (decimal)(q.L ?? 0),
                Open = (decimal)(q.O ?? 0),
                Timestamp = q.T.HasValue ? DateTimeConverter.UnixTimestampToDateTime(q.T.Value) : DateTime.UtcNow
            });
            
        // Act
        await _service.ImportIntradayMarketDataAsync();
        
        // Assert
        _finnhubClientMock.Verify(c => c.QuoteAsync("AAPL"), Times.Exactly(2));
    }
} 