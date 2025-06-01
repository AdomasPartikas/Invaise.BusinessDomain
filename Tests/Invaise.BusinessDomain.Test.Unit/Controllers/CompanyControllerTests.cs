using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class CompanyControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly CompanyController _controller;
    private readonly User _testUser;

    public CompanyControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _controller = new CompanyController(_dbServiceMock.Object);

        // Set up a test user
        _testUser = new User
        {
            Id = "user1",
            DisplayName = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            IsActive = true
        };

        // Set up HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.Items = new Dictionary<object, object?>
        {
            { "User", _testUser },
            { "ServiceAccount", null }
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetCompanies_ReturnsOkWithCompanies()
    {
        // Arrange
        var companies = new List<Company>
        {
            new Company { StockId = 1, Symbol = "AAPL", Name = "Apple Inc." },
            new Company { StockId = 2, Symbol = "MSFT", Name = "Microsoft Corporation" }
        };

        _dbServiceMock.Setup(db => db.GetAllCompaniesAsync())
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.GetCompanies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCompanies = Assert.IsAssignableFrom<IEnumerable<Company>>(okResult.Value);
        Assert.Equal(2, returnedCompanies.Count());
    }

    [Fact]
    public async Task GetCompanyById_ExistingId_ReturnsCompany()
    {
        // Arrange
        var companyId = 1;
        var company = new Company
        {
            StockId = companyId,
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Industry = "Technology",
            Country = "USA"
        };

        _dbServiceMock.Setup(db => db.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(company);

        // Act
        var result = await _controller.GetCompanyById(companyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCompany = Assert.IsType<Company>(okResult.Value);
        Assert.Equal(companyId, returnedCompany.StockId);
        Assert.Equal("AAPL", returnedCompany.Symbol);
    }

    [Fact]
    public async Task GetCompanyById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var companyId = 999;

        _dbServiceMock.Setup(db => db.GetCompanyByIdAsync(companyId))
            .ReturnsAsync((Company?)null);

        // Act
        var result = await _controller.GetCompanyById(companyId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCompanyBySymbol_ExistingSymbol_ReturnsCompany()
    {
        // Arrange
        var symbol = "AAPL";
        var company = new Company
        {
            StockId = 1,
            Symbol = symbol,
            Name = "Apple Inc.",
            Industry = "Technology",
            Country = "USA"
        };

        _dbServiceMock.Setup(db => db.GetCompanyBySymbolAsync(symbol))
            .ReturnsAsync(company);

        // Act
        var result = await _controller.GetCompanyBySymbol(symbol);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCompany = Assert.IsType<Company>(okResult.Value);
        Assert.Equal(symbol, returnedCompany.Symbol);
    }

    [Fact]
    public async Task GetCompanyBySymbol_NonExistingSymbol_ReturnsNotFound()
    {
        // Arrange
        var symbol = "INVALID";

        _dbServiceMock.Setup(db => db.GetCompanyBySymbolAsync(symbol))
            .ReturnsAsync((Company?)null);

        // Act
        var result = await _controller.GetCompanyBySymbol(symbol);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateCompany_ValidRequest_ReturnsCreatedCompany()
    {
        // Arrange
        var request = new CompanyController.CreateCompanyRequest
        {
            Symbol = "GOOG",
            Name = "Alphabet Inc.",
            Industry = "Technology",
            Description = "Google parent company",
            Country = "USA"
        };

        var createdCompany = new Company
        {
            StockId = 3,
            Symbol = request.Symbol,
            Name = request.Name,
            Industry = request.Industry,
            Description = request.Description,
            Country = request.Country
        };

        _dbServiceMock.Setup(db => db.GetCompanyBySymbolAsync(request.Symbol))
            .ReturnsAsync((Company?)null);

        _dbServiceMock.Setup(db => db.CreateCompanyAsync(It.IsAny<Company>()))
            .ReturnsAsync(createdCompany);

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetCompanyById", createdAtActionResult.ActionName);
        Assert.Equal(createdCompany.StockId, createdAtActionResult.RouteValues?["id"]);

        var returnedCompany = Assert.IsType<Company>(createdAtActionResult.Value);
        Assert.Equal(request.Symbol, returnedCompany.Symbol);
        Assert.Equal(request.Name, returnedCompany.Name);
    }

    [Fact]
    public async Task CreateCompany_DuplicateSymbol_ReturnsConflict()
    {
        // Arrange
        var request = new CompanyController.CreateCompanyRequest
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Industry = "Technology",
            Country = "USA"
        };

        var existingCompany = new Company
        {
            StockId = 1,
            Symbol = request.Symbol,
            Name = "Apple Inc."
        };

        _dbServiceMock.Setup(db => db.GetCompanyBySymbolAsync(request.Symbol))
            .ReturnsAsync(existingCompany);

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
        // Use GetType().GetProperty() instead of direct cast to object
        var messageProperty = conflictResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(conflictResult.Value)?.ToString();
        Assert.Contains($"A company with symbol '{request.Symbol}' already exists", message);
    }

    [Fact]
    public async Task UpdateCompany_ExistingCompany_ReturnsUpdatedCompany()
    {
        // Arrange
        var companyId = 1;
        var existingCompany = new Company
        {
            StockId = companyId,
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Industry = "Technology",
            Description = "Makes iPhones",
            Country = "USA"
        };

        var request = new CompanyController.UpdateCompanyRequest
        {
            Name = "Apple Incorporated",
            Description = "Makes iPhones and MacBooks"
        };

        var updatedCompany = new Company
        {
            StockId = companyId,
            Symbol = "AAPL",
            Name = request.Name ?? existingCompany.Name,
            Industry = request.Industry ?? existingCompany.Industry,
            Description = request.Description ?? existingCompany.Description,
            Country = request.Country ?? existingCompany.Country
        };

        _dbServiceMock.Setup(db => db.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(existingCompany);

        _dbServiceMock.Setup(db => db.UpdateCompanyAsync(It.IsAny<Company>()))
            .ReturnsAsync(updatedCompany);

        // Act
        var result = await _controller.UpdateCompany(companyId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCompany = Assert.IsType<Company>(okResult.Value);
        Assert.Equal(companyId, returnedCompany.StockId);
        Assert.Equal(request.Name, returnedCompany.Name);
        Assert.Equal(request.Description, returnedCompany.Description);
    }

    [Fact]
    public async Task UpdateCompany_NonExistingCompany_ReturnsNotFound()
    {
        // Arrange
        var companyId = 999;
        var request = new CompanyController.UpdateCompanyRequest
        {
            Name = "Updated Name"
        };

        _dbServiceMock.Setup(db => db.GetCompanyByIdAsync(companyId))
            .ReturnsAsync((Company?)null);

        // Act
        var result = await _controller.UpdateCompany(companyId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCompany_ExistingCompany_ReturnsSuccess()
    {
        // Arrange
        var companyId = 1;

        _dbServiceMock.Setup(db => db.DeleteCompanyAsync(companyId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCompany(companyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // Use GetType().GetProperty() instead of direct cast to object
        var messageProperty = okResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(okResult.Value)?.ToString();
        Assert.Equal("Company deleted successfully", message);
    }

    [Fact]
    public async Task DeleteCompany_NonExistingCompany_ReturnsNotFound()
    {
        // Arrange
        var companyId = 999;

        _dbServiceMock.Setup(db => db.DeleteCompanyAsync(companyId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCompany(companyId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
} 