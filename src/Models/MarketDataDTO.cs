namespace Invaise.BusinessDomain.API.Models;

/// <summary>
/// Data Transfer Object (DTO) for market data.
/// </summary>
public class MarketDataDto
{

    /// <summary>
    /// Gets or sets the symbol of the financial instrument (e.g., stock ticker).
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of the market data entry.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the opening price of the financial instrument.
    /// </summary>
    public decimal? Open { get; set; }

    /// <summary>
    /// Gets or sets the highest price of the financial instrument during the day.
    /// </summary>
    public decimal? High { get; set; }

    /// <summary>
    /// Gets or sets the lowest price of the financial instrument during the day.
    /// </summary>
    public decimal? Low { get; set; }

    /// <summary>
    /// Gets or sets the closing price of the financial instrument.
    /// </summary>
    public decimal? Close { get; set; }

    /// <summary>
    /// Gets or sets the trading volume of the financial instrument.
    /// </summary>
    public double? Volume { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the market data entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}