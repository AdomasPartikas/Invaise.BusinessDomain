using Invaise.BusinessDomain.API.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling market data-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly InvaiseDbContext context;

    public MarketDataController(InvaiseDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Retrieves market data based on the provided query parameters.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data.</param>
    /// <param name="start">The start date for the market data range.</param>
    /// <param name="end">The end date for the market data range.</param>
    /// <returns>A list of market data matching the query parameters.</returns>
    [HttpGet("GetMarketData")]
    [ProducesResponseType(typeof(IEnumerable<MarketData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketData([FromQuery] string symbol, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var query = context.MarketData.AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(m => m.Symbol == symbol);

        if (start.HasValue)
            query = query.Where(m => m.Date >= start.Value);

        if (end.HasValue)
            query = query.Where(m => m.Date <= end.Value);

        var results = await query.OrderBy(m => m.Date).ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Retrieves all unique symbols from the market data.
    /// /// </summary>
    /// <returns>A list of unique symbols.</returns>
    [HttpGet("GetAllUniqueSymbols")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUniqueSymbols()
    {
        var symbols = await context.MarketData
            .Select(m => m.Symbol)
            .Distinct()
            .ToListAsync();

        return Ok(symbols);
    }
}
