using Invaise.BusinessDomain.API.Controllers;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Invaise.BusinessDomain.API.Controllers.TransactionController;

namespace Invaise.BusinessDomain.Test.Unit.Controllers;

public class TransactionControllerTests : TestBase
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly TransactionController _controller;
    private readonly User _testUser;

    public TransactionControllerTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _controller = new TransactionController(_dbServiceMock.Object);
        
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        _testUser = new User
        {
            Id = "user-id",
            DisplayName = "Test User",
            Email = "test@example.com",
            Role = "User"
        };
        
        _controller.HttpContext.Items["User"] = _testUser;
    }

    [Fact]
    public async Task GetUserTransactions_ReturnsOkWithTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { 
                Id = "1", 
                UserId = _testUser.Id, 
                Symbol = "AAPL", 
                Quantity = 10, 
                PricePerShare = 150.00m, 
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Buy,
                TriggeredBy = AvailableTransactionTriggers.User
            },
            new() { 
                Id = "2", 
                UserId = _testUser.Id, 
                Symbol = "MSFT", 
                Quantity = 5, 
                PricePerShare = 200.00m, 
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Buy,
                TriggeredBy = AvailableTransactionTriggers.User
            }
        };
        
        _dbServiceMock.Setup(s => s.GetUserTransactionsAsync(_testUser.Id))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetUserTransactions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<Transaction>>(okResult.Value);
        Assert.Equal(2, returnedTransactions.Count());
    }

    [Fact]
    public async Task GetPortfolioTransactions_PortfolioExists_UserHasAccess_ReturnsOkWithTransactions()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var transactions = new List<Transaction>
        {
            new() { 
                Id = "1", 
                PortfolioId = portfolioId, 
                Symbol = "AAPL", 
                Quantity = 10, 
                PricePerShare = 150.00m, 
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Buy,
                TriggeredBy = AvailableTransactionTriggers.User
            },
            new() { 
                Id = "2", 
                PortfolioId = portfolioId, 
                Symbol = "MSFT", 
                Quantity = 5, 
                PricePerShare = 200.00m, 
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Buy,
                TriggeredBy = AvailableTransactionTriggers.User
            }
        };
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
            
        _dbServiceMock.Setup(s => s.GetPortfolioTransactionsAsync(portfolioId))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetPortfolioTransactions(portfolioId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<Transaction>>(okResult.Value);
        Assert.Equal(2, returnedTransactions.Count());
    }

    [Fact]
    public async Task GetPortfolioTransactions_PortfolioExists_AdminAccess_ReturnsOkWithTransactions()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        string otherUserId = "other-user-id";
        
        var adminUser = new User
        {
            Id = "admin-id",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Role = "Admin"
        };
        
        _controller.HttpContext.Items["User"] = adminUser;
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = otherUserId, // Not the admin's portfolio
            Name = "Test Portfolio"
        };
        
        var transactions = new List<Transaction>
        {
            new() { 
                Id = "1", 
                PortfolioId = portfolioId, 
                Symbol = "AAPL", 
                Quantity = 10, 
                PricePerShare = 150.00m, 
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Buy,
                TriggeredBy = AvailableTransactionTriggers.User
            },
            new() { 
                Id = "2", 
                PortfolioId = portfolioId, 
                Symbol = "MSFT", 
                Quantity = 5, 
                PricePerShare = 200.00m, 
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Buy,
                TriggeredBy = AvailableTransactionTriggers.User
            }
        };
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
            
        _dbServiceMock.Setup(s => s.GetPortfolioTransactionsAsync(portfolioId))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetPortfolioTransactions(portfolioId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<Transaction>>(okResult.Value);
        Assert.Equal(2, returnedTransactions.Count());
    }

    [Fact]
    public async Task GetPortfolioTransactions_PortfolioNotFound_ReturnsNotFound()
    {
        // Arrange
        string portfolioId = "non-existent-portfolio";
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio?)null);

        // Act
        var result = await _controller.GetPortfolioTransactions(portfolioId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Portfolio not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetPortfolioTransactions_UserNoAccess_ReturnsForbid()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        string otherUserId = "other-user-id";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = otherUserId, // Not the current user's portfolio
            Name = "Test Portfolio"
        };
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);

        // Act
        var result = await _controller.GetPortfolioTransactions(portfolioId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateTransaction_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = _testUser.Id,
            Name = "Test Portfolio"
        };
        
        var request = new CreateTransactionRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m,
            Type = TransactionType.Buy
        };
        
        var createdTransaction = new Transaction
        {
            Id = "new-transaction-id",
            PortfolioId = portfolioId,
            UserId = _testUser.Id,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m,
            TransactionValue = 1500.00m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);
            
        _dbServiceMock.Setup(s => s.CreateTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(createdTransaction);

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransactionController.GetUserTransactions), createdAtActionResult.ActionName);
        
        var returnedTransaction = Assert.IsType<Transaction>(createdAtActionResult.Value);
        Assert.Equal(createdTransaction.Id, returnedTransaction.Id);
    }

    [Fact]
    public async Task CreateTransaction_PortfolioNotFound_ReturnsNotFound()
    {
        // Arrange
        string portfolioId = "non-existent-portfolio";
        
        var request = new CreateTransactionRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m
        };
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync((Portfolio?)null);

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Portfolio not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CreateTransaction_UserNoAccess_ReturnsForbid()
    {
        // Arrange
        string portfolioId = "portfolio-id";
        string otherUserId = "other-user-id";
        
        var portfolio = new Portfolio
        {
            Id = portfolioId,
            UserId = otherUserId, // Not the current user's portfolio
            Name = "Test Portfolio"
        };
        
        var request = new CreateTransactionRequest
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m
        };
        
        _dbServiceMock.Setup(s => s.GetPortfolioByIdAsync(portfolioId))
            .ReturnsAsync(portfolio);

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CancelTransaction_TransactionExists_UserHasAccess_ReturnsNoContent()
    {
        // Arrange
        string transactionId = "transaction-id";
        
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = _testUser.Id,
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(s => s.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(transaction);
            
        _dbServiceMock.Setup(s => s.CancelTransactionAsync(transactionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CancelTransaction(transactionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CancelTransaction_TransactionExists_AdminAccess_ReturnsNoContent()
    {
        // Arrange
        string transactionId = "transaction-id";
        string otherUserId = "other-user-id";
        
        var adminUser = new User
        {
            Id = "admin-id",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Role = "Admin"
        };
        
        _controller.HttpContext.Items["User"] = adminUser;
        
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = otherUserId, // Not the admin's transaction
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(s => s.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(transaction);
            
        _dbServiceMock.Setup(s => s.CancelTransactionAsync(transactionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CancelTransaction(transactionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CancelTransaction_TransactionNotFound_ReturnsNotFound()
    {
        // Arrange
        string transactionId = "non-existent-transaction";
        
        _dbServiceMock.Setup(s => s.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _controller.CancelTransaction(transactionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Transaction not found", notFoundResult.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task CancelTransaction_UserNoAccess_ReturnsForbid()
    {
        // Arrange
        string transactionId = "transaction-id";
        string otherUserId = "other-user-id";
        
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = otherUserId, // Not the current user's transaction
            Symbol = "AAPL",
            Quantity = 10,
            PricePerShare = 150.00m,
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Buy,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        _dbServiceMock.Setup(s => s.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.CancelTransaction(transactionId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
} 