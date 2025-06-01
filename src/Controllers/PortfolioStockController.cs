using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling portfolio stock operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioStockController(IDatabaseService dbService) : ControllerBase
{
    /// <summary>
    /// Gets all stocks for a specific portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio stocks.</returns>
    [HttpGet("portfolio/{portfolioId}")]
    public async Task<IActionResult> GetPortfolioStocks(string portfolioId)
    {
        var isServiceAccount = HttpContext.Items["ServiceAccount"] != null;
        var currentUser = (User)HttpContext.Items["User"]!;

        if (!isServiceAccount && currentUser == null)
            return Unauthorized(new { message = "Unauthorized" });

        var portfolio = await dbService.GetPortfolioByIdAsync(portfolioId);
        
        if (portfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        // Ensure user can only access their own portfolios
        if (!isServiceAccount && portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        var stocks = await dbService.GetPortfolioStocksAsync(portfolioId);
        return Ok(stocks);
    }
    
    /// <summary>
    /// Gets a specific stock in a portfolio.
    /// </summary>
    /// <param name="id">The portfolio stock ID.</param>
    /// <returns>The portfolio stock information.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPortfolioStock(string id)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var portfolioStock = await dbService.GetPortfolioStockByIdAsync(id);
        
        if (portfolioStock == null)
            return NotFound(new { message = "Portfolio stock not found" });
            
        // Ensure user can only access their own portfolio stocks
        if (portfolioStock.Portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        return Ok(portfolioStock);
    }
    
    /// <summary>
    /// Represents the request to create a portfolio stock.
    /// </summary>
    public class CreatePortfolioStockRequest
    {
        /// <summary>
        /// The ID of the portfolio to which the stock belongs.
        /// </summary>
        public required string PortfolioId { get; set; }

        /// <summary>
        /// The symbol of the stock.
        /// </summary>
        public required string Symbol { get; set; }

        /// <summary>
        /// The quantity of the stock.
        /// </summary>
        public required decimal Quantity { get; set; }

        /// <summary>
        /// The current total value of the stock.
        /// </summary>
        public required decimal CurrentTotalValue { get; set; }

        /// <summary>
        /// The total base value of the stock.
        /// </summary>
        public required decimal TotalBaseValue { get; set; }

        /// <summary>
        /// The percentage change in the stock value.
        /// </summary>
        public decimal PercentageChange { get; set; }
    }
    
    /// <summary>
    /// Adds a stock to a portfolio.
    /// </summary>
    /// <param name="request">The portfolio stock creation request.</param>
    /// <returns>The created portfolio stock.</returns>
    [HttpPost]
    public async Task<IActionResult> AddStockToPortfolio([FromBody] CreatePortfolioStockRequest request)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var portfolio = await dbService.GetPortfolioByIdAsync(request.PortfolioId);
        
        if (portfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        // Ensure user can only add stocks to their own portfolios
        if (portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();

        // Check if the stock already exists in the portfolio
        var existingStock = await dbService.GetPortfolioStocksAsync(request.PortfolioId);

        if (existingStock.Any(s => s.Symbol == request.Symbol))
        {
            existingStock.First(s => s.Symbol == request.Symbol).Quantity += request.Quantity;
            existingStock.First(s => s.Symbol == request.Symbol).CurrentTotalValue += request.CurrentTotalValue;
            existingStock.First(s => s.Symbol == request.Symbol).TotalBaseValue += request.TotalBaseValue;
            existingStock.First(s => s.Symbol == request.Symbol).PercentageChange = request.PercentageChange;
            existingStock.First(s => s.Symbol == request.Symbol).LastUpdated = DateTime.UtcNow.ToLocalTime();

            var updatedStock = await dbService.UpdatePortfolioStockAsync(existingStock.First(s => s.Symbol == request.Symbol));
            return Ok(updatedStock);
        }
        else
        {
            var portfolioStock = new PortfolioStock
            {
                PortfolioId = request.PortfolioId,
                Symbol = request.Symbol,
                Quantity = request.Quantity,
                CurrentTotalValue = request.CurrentTotalValue,
                TotalBaseValue = request.TotalBaseValue,
                PercentageChange = request.PercentageChange,
                LastUpdated = DateTime.UtcNow.ToLocalTime(),
                Portfolio = portfolio
            };

            var createdStock = await dbService.AddPortfolioStockAsync(portfolioStock);
            return CreatedAtAction(nameof(GetPortfolioStock), new { id = createdStock.ID }, createdStock);
        }
    }
    
    /// <summary>
    /// Represents the request to update a portfolio stock.
    /// </summary>
    public class UpdatePortfolioStockRequest
    {
        /// <summary>
        /// The quantity of the stock.
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// The current total value of the stock.
        /// </summary>
        public decimal? CurrentTotalValue { get; set; }

        /// <summary>
        /// The total base value of the stock.
        /// </summary>
        public decimal? TotalBaseValue { get; set; }

        /// <summary>
        /// The percentage change in the stock value.
        /// </summary>
        public decimal? PercentageChange { get; set; }
    }
    
    /// <summary>
    /// Updates a portfolio stock.
    /// </summary>
    /// <param name="id">The portfolio stock ID.</param>
    /// <param name="request">The updated portfolio stock information.</param>
    /// <returns>The updated portfolio stock.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePortfolioStock(string id, [FromBody] UpdatePortfolioStockRequest request)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var existingStock = await dbService.GetPortfolioStockByIdAsync(id);
        
        if (existingStock == null)
            return NotFound(new { message = "Portfolio stock not found" });
            
        // Ensure user can only update their own portfolio stocks
        if (existingStock.Portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        // Update stock properties if provided
        if (request.Quantity.HasValue)
            existingStock.Quantity = request.Quantity.Value;
            
        if (request.CurrentTotalValue.HasValue)
            existingStock.CurrentTotalValue = request.CurrentTotalValue.Value;
            
        if (request.TotalBaseValue.HasValue)
            existingStock.TotalBaseValue = request.TotalBaseValue.Value;
            
        if (request.PercentageChange.HasValue)
            existingStock.PercentageChange = request.PercentageChange.Value;
            
        existingStock.LastUpdated = DateTime.UtcNow.ToLocalTime();
        
        var updatedStock = await dbService.UpdatePortfolioStockAsync(existingStock);
        return Ok(updatedStock);
    }
    
    /// <summary>
    /// Deletes a portfolio stock.
    /// </summary>
    /// <param name="id">The portfolio stock ID.</param>
    /// <returns>A success message.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePortfolioStock(string id)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var existingStock = await dbService.GetPortfolioStockByIdAsync(id);
        
        if (existingStock == null)
            return NotFound(new { message = "Portfolio stock not found" });
            
        // Ensure user can only delete their own portfolio stocks
        if (existingStock.Portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        var result = await dbService.DeletePortfolioStockAsync(id);
        
        if (!result)
            return StatusCode(500, new { message = "Failed to delete portfolio stock" });
            
        return Ok(new { message = "Portfolio stock deleted successfully" });
    }
} 