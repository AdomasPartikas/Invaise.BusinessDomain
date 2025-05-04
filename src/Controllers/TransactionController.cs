using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Enums;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling transaction operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController(IDatabaseService dbService) : ControllerBase
{
    /// <summary>
    /// Gets all transactions for the current user.
    /// </summary>
    /// <returns>The user's transactions.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUserTransactions()
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var transactions = await dbService.GetUserTransactionsAsync(currentUser.Id);
        return Ok(transactions);
    }

    /// <summary>
    /// Gets all transactions for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio's transactions.</returns>
    [HttpGet("portfolio/{portfolioId}")]
    public async Task<IActionResult> GetPortfolioTransactions(string portfolioId)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // First check if the user has access to the portfolio
        var portfolio = await dbService.GetPortfolioByIdAsync(portfolioId);
        
        if (portfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        if (portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        var transactions = await dbService.GetPortfolioTransactionsAsync(portfolioId);
        return Ok(transactions);
    }

    public class CreateTransactionRequest
    {
        public string PortfolioId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PricePerShare { get; set; }
        public TransactionType Type { get; set; } = TransactionType.Buy;
        public DateTime? TransactionDate { get; set; }
    }

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="request">The transaction creation request.</param>
    /// <returns>The created transaction.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // Check if the portfolio exists and belongs to the user
        var portfolio = await dbService.GetPortfolioByIdAsync(request.PortfolioId);
        
        if (portfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        if (portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        var transaction = new Transaction
        {
            UserId = currentUser.Id,
            PortfolioId = request.PortfolioId,
            Symbol = request.Symbol,
            Quantity = request.Quantity,
            PricePerShare = request.PricePerShare,
            TransactionValue = request.Quantity * request.PricePerShare,
            TransactionDate = request.TransactionDate ?? DateTime.UtcNow.ToLocalTime(),
            Type = request.Type,
            TriggeredBy = AvailableTransactionTriggers.User
        };
        
        var createdTransaction = await dbService.CreateTransactionAsync(transaction);
        return CreatedAtAction(nameof(GetUserTransactions), createdTransaction);
    }

    /// <summary>
    /// Cancels a transaction.
    /// /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    [HttpDelete("{transactionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelTransaction(string transactionId)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // Check if the transaction exists and belongs to the user
        var transaction = await dbService.GetTransactionByIdAsync(transactionId);
        
        if (transaction == null)
            return NotFound(new { message = "Transaction not found" });
            
        if (transaction.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        await dbService.CancelTransactionAsync(transactionId);
        
        return NoContent();
    }
} 