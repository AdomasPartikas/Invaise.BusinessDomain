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
public class MarketDataController(IDatabaseService dbService, IMarketDataService marketDataService) : ControllerBase
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


    /// <summary>
    /// Retrieves the latest intraday market data for the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data.</param>
    /// <returns>The latest intraday market data for the specified symbol.</returns>
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

    /// <summary>
    /// Retrieves the latest intraday market data for the specified symbol with a specified count.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data.</param>
    /// <param name="count">The number of latest intraday market data entries to retrieve.</param>
    /// <returns>A list of intraday market data matching the query parameters.</returns>
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

    /// <summary>
    /// Retrieves the latest historical market data for the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data.</param>
    /// <returns>The latest historical market data for the specified symbol.</returns>
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

    /// <summary>
    /// Retrieves the latest historical market data for the specified symbol with a specified count.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data.</param>
    /// <param name="count">The number of latest historical market data entries to retrieve.</param>
    /// <returns>A list of historical market data matching the query parameters.</returns>
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

    /// <summary>
    /// Checks if the market is currently open.
    /// </summary>
    /// <returns>A boolean indicating whether the market is open.</returns>
    [HttpGet("IsMarketOpen")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsMarketOpen()
    {
        var isOpen = await marketDataService.IsMarketOpenAsync();

        return Ok(isOpen);
    }
}
