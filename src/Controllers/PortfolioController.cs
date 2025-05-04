using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling portfolio operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController(IDatabaseService dbService, IPortfolioService portfolioService) : ControllerBase
{   
    /// <summary>
    /// Gets all portfolios for the current user.
    /// </summary>
    /// <returns>The user's portfolios.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUserPortfolios()
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var portfolios = await dbService.GetUserPortfoliosAsync(currentUser.Id);
        return Ok(portfolios);
    }
    
    /// <summary>
    /// Gets a portfolio by its ID.
    /// </summary>
    /// <param name="id">The portfolio ID.</param>
    /// <returns>The portfolio information.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPortfolio(string id)
    {
        var isServiceAccount = HttpContext.Items["ServiceAccount"] != null;
        var currentUser = HttpContext.Items["User"] as User;
        
        // If this is a service account, we can proceed without user validation
        // If this is a regular user account, ensure the user is valid
        if (!isServiceAccount && currentUser == null)
            return Unauthorized(new { message = "Unauthorized" });
            
        var portfolio = await dbService.GetPortfolioByIdAsync(id);
        
        if (portfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        // If this is a regular user account, ensure they can only access their own portfolios
        if (!isServiceAccount && portfolio.UserId != currentUser!.Id && currentUser!.Role != "Admin")
            return Forbid();
            
        return Ok(portfolio);
    }
    
    public class CreatePortfolioRequest
    {
        public string Name { get; set; } = "New Portfolio";
        public PortfolioStrategy StrategyDescription { get; set; } = PortfolioStrategy.Balanced;
    }
    
    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="request">The portfolio creation request.</param>
    /// <returns>The created portfolio.</returns>
    [HttpPost]
    public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioRequest request)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        var portfolio = new Portfolio
        {
            UserId = currentUser.Id,
            Name = request.Name,
            StrategyDescription = request.StrategyDescription
        };
        
        var createdPortfolio = await dbService.CreatePortfolioAsync(portfolio);
        return CreatedAtAction(nameof(GetPortfolio), new { id = createdPortfolio.Id }, createdPortfolio);
    }
    
    public class UpdatePortfolioRequest
    {
        public string Name { get; set; } = string.Empty;
        public PortfolioStrategy StrategyDescription { get; set; }
    }
    
    /// <summary>
    /// Updates a portfolio.
    /// </summary>
    /// <param name="id">The portfolio ID.</param>
    /// <param name="request">The updated portfolio information.</param>
    /// <returns>The updated portfolio.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePortfolio(string id, [FromBody] UpdatePortfolioRequest request)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var existingPortfolio = await dbService.GetPortfolioByIdAsync(id);
        
        if (existingPortfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        // Ensure user can only update their own portfolios
        if (existingPortfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        // Update portfolio properties
        existingPortfolio.Name = !string.IsNullOrEmpty(request.Name) ? request.Name : existingPortfolio.Name;
        existingPortfolio.StrategyDescription = request.StrategyDescription;
        
        var updatedPortfolio = await dbService.UpdatePortfolioAsync(existingPortfolio);
        return Ok(updatedPortfolio);
    }
    
    /// <summary>
    /// Deletes a portfolio.
    /// </summary>
    /// <param name="id">The portfolio ID.</param>
    /// <returns>A success message.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePortfolio(string id)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        var existingPortfolio = await dbService.GetPortfolioByIdAsync(id);
        
        if (existingPortfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        // Ensure user can only delete their own portfolios
        if (existingPortfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
            
        var result = await dbService.DeletePortfolioAsync(id);
        
        if (!result)
            return StatusCode(500, new { message = "Failed to delete portfolio" });
            
        return Ok(new { message = "Portfolio deleted successfully" });
    }
    
    /// <summary>
    /// Generates a PDF report of portfolio performance for a specific date range.
    /// </summary>
    /// <param name="id">The portfolio ID.</param>
    /// <param name="startDate">The start date for performance data.</param>
    /// <param name="endDate">The end date for performance data.</param>
    /// <returns>The generated PDF file.</returns>
    [HttpGet("{id}/performance-report")]
    public async Task<IActionResult> GeneratePortfolioPerformanceReport(string id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // Check if portfolio exists
        var portfolio = await dbService.GetPortfolioByIdAsync(id);
        
        if (portfolio == null)
            return NotFound(new { message = "Portfolio not found" });
            
        // Ensure the user can only access their own portfolios
        if (portfolio.UserId != currentUser.Id && currentUser.Role != "Admin")
            return Forbid();
        
        try
        {
            // Generate the PDF report
            var pdfBytes = await portfolioService.GeneratePortfolioPerformancePdfAsync(
                currentUser.Id, 
                id, 
                startDate, 
                endDate);
            
            // Return the PDF file
            return File(
                pdfBytes,
                "application/pdf",
                $"portfolio-performance-report-{id}-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error generating PDF report: {ex.Message}" });
        }
    }
} 