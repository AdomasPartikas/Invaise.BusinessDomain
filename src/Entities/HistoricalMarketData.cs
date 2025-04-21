using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents market data for a specific financial instrument.
/// </summary>
public class HistoricalMarketData
{
    /// <summary>
    /// Gets or sets the unique identifier for the market data entry.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the symbol of the financial instrument (e.g., stock ticker).
    /// </summary>
    [Required]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of the market data entry.
    /// </summary>
    [Required]
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
    public long? Volume { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the market data entry was created.
    /// Defaults to the current UTC time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

