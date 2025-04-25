using Invaise.BusinessDomain.API.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.AspNetCore.Mvc.Infrastructure;

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
    [HttpGet("GetHistoricalMarketData")]
    [ProducesResponseType(typeof(IEnumerable<HistoricalMarketData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoricalMarketData([FromQuery] string symbol, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        var results = await dbService.GetHistoricalMarketDataAsync(symbol, start, end);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves intraday market data based on the provided query parameters.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data.</param>
    /// <param name="start">The start date for the market data range.</param>
    /// <param name="end">The end date for the market data range.</param>
    /// <returns> A list of intraday market data matching the query parameters.</returns>
    [HttpGet("GetIntradayMarketData")]
    [ProducesResponseType(typeof(IEnumerable<IntradayMarketData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIntradayMarketData([FromQuery] string symbol, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        var results = await dbService.GetIntradayMarketDataAsync(symbol, start, end);

        return Ok(results);
    }


    [HttpGet("GetLatestIntradayMarketData")]
    [ProducesResponseType(typeof(IntradayMarketData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatestIntradayMarketData([FromQuery] string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        var results = await dbService.GetLatestIntradayMarketDataAsync(symbol);

        return Ok(results);
    }

    [HttpGet("GetLatestIntradayMarketDataWithCount")]
    [ProducesResponseType(typeof(IEnumerable<IntradayMarketData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatestIntradayMarketDataWithCount([FromQuery] string symbol, [FromQuery] int count)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        if (count <= 0)
            return BadRequest("Count must be greater than zero.");

        var results = await dbService.GetLatestIntradayMarketDataAsync(symbol, count);

        return Ok(results);
    }

    [HttpGet("GetLatestHistoricalMarketData")]
    [ProducesResponseType(typeof(HistoricalMarketData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatestHistoricalMarketData([FromQuery] string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        var results = await dbService.GetLatestHistoricalMarketDataAsync(symbol);

        return Ok(results);
    }

    [HttpGet("GetLatestHistoricalMarketDataWithCount")]
    [ProducesResponseType(typeof(HistoricalMarketData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatestHistoricalMarketDataWithCount([FromQuery] string symbol, [FromQuery] int count)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        if( count <= 0)
            return BadRequest("Count must be greater than zero.");

        var results = await dbService.GetLatestHistoricalMarketDataAsync(symbol, count);

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
