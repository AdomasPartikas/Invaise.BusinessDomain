using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents market data for a specific financial instrument.
/// </summary>
public class IntradayMarketData
{
    /// <summary>
    /// Represents market data for a specific financial instrument.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the market data entry.
    /// </summary>
    [Required]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of the market data entry.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the opening price of the financial instrument this day.
    /// </summary>
    [Required]
    public decimal Open { get; set; }

    /// <summary>
    /// Gets or sets the highest price of the financial instrument this day.
    /// </summary>
    [Required]
    public decimal High { get; set; }

    /// <summary>
    /// Gets or sets the lowest price of the financial instrument this day.
    /// </summary>
    [Required]
    public decimal Low { get; set; }

    /// <summary>
    /// Gets or sets the current price of the financial instrument.
    /// </summary>
    [Required]
    public decimal Current { get; set; }
}