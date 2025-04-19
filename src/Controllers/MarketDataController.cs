using Invaise.BusinessDomain.API.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Invaise.BusinessDomain.API.Interfaces;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling market data-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MarketDataController(IDatabaseService dbService) : ControllerBase
{

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
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        var results = await dbService.GetMarketDataAsync(symbol, start, end);

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
        var symbols = await dbService.GetAllUniqueMarketDataSymbolsAsync();

        if (symbols == null || !symbols.Any())
            return NotFound("No symbols found.");

        return Ok(symbols);
    }
}
