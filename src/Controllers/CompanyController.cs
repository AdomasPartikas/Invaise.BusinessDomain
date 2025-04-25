using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling company operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController(IDatabaseService dbService) : ControllerBase
{
    /// <summary>
    /// Gets all companies.
    /// </summary>
    /// <returns>A collection of companies.</returns>
    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await dbService.GetAllCompaniesAsync();
        return Ok(companies);
    }
    
    /// <summary>
    /// Gets a company by its ID.
    /// </summary>
    /// <param name="id">The company ID.</param>
    /// <returns>The company information.</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCompanyById(int id)
    {
        var company = await dbService.GetCompanyByIdAsync(id);
        
        if (company == null)
            return NotFound(new { message = "Company not found" });
            
        return Ok(company);
    }
    
    /// <summary>
    /// Gets a company by its stock symbol.
    /// </summary>
    /// <param name="symbol">The stock symbol.</param>
    /// <returns>The company information.</returns>
    [HttpGet("symbol/{symbol}")]
    public async Task<IActionResult> GetCompanyBySymbol(string symbol)
    {
        var company = await dbService.GetCompanyBySymbolAsync(symbol);
        
        if (company == null)
            return NotFound(new { message = "Company not found" });
            
        return Ok(company);
    }
    
    public class CreateCompanyRequest
    {
        public required string Symbol { get; set; }
        public required string Name { get; set; }
        public string? Industry { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string Country { get; set; }
    }
    
    /// <summary>
    /// Creates a new company.
    /// </summary>
    /// <param name="request">The company creation request.</param>
    /// <returns>The created company.</returns>
    [HttpPost]
    [Authorize("Admin")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        // Check if a company with this symbol already exists
        var existingCompany = await dbService.GetCompanyBySymbolAsync(request.Symbol);
        if (existingCompany != null)
            return Conflict(new { message = $"A company with symbol '{request.Symbol}' already exists" });
            
        var company = new Company
        {
            Symbol = request.Symbol,
            Name = request.Name,
            Industry = request.Industry,
            Description = request.Description,
            Country = request.Country
        };
        
        var createdCompany = await dbService.CreateCompanyAsync(company);
        return CreatedAtAction(nameof(GetCompanyById), new { id = createdCompany.StockId }, createdCompany);
    }
    
    public class UpdateCompanyRequest
    {
        public string? Name { get; set; }
        public string? Industry { get; set; }
        public string? Description { get; set; }
        public string? Country { get; set; }
    }
    
    /// <summary>
    /// Updates a company.
    /// </summary>
    /// <param name="id">The company ID.</param>
    /// <param name="request">The updated company information.</param>
    /// <returns>The updated company.</returns>
    [HttpPut("{id:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyRequest request)
    {
        var existingCompany = await dbService.GetCompanyByIdAsync(id);
        
        if (existingCompany == null)
            return NotFound(new { message = "Company not found" });
            
        // Update company properties if provided
        if (!string.IsNullOrEmpty(request.Name))
            existingCompany.Name = request.Name;
            
        if (request.Industry != null)
            existingCompany.Industry = request.Industry;
            
        if (request.Description != null)
            existingCompany.Description = request.Description;
            
        if (!string.IsNullOrEmpty(request.Country))
            existingCompany.Country = request.Country;
        
        var updatedCompany = await dbService.UpdateCompanyAsync(existingCompany);
        return Ok(updatedCompany);
    }
    
    /// <summary>
    /// Deletes a company.
    /// </summary>
    /// <param name="id">The company ID.</param>
    /// <returns>A success message.</returns>
    [HttpDelete("{id:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var result = await dbService.DeleteCompanyAsync(id);
        
        if (!result)
            return NotFound(new { message = "Company not found" });
            
        return Ok(new { message = "Company deleted successfully" });
    }
} 