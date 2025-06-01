using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;
/// <summary>
/// Represents a stock within a portfolio, including details such as quantity, value, and percentage change.
/// </summary>
public class PortfolioStock
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio stock.
    /// </summary>
    [Key]
    public string ID { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the unique identifier for the portfolio associated with this stock.
    /// </summary>
    [Required]
    [ForeignKey("Portfolio")]
    public required string PortfolioId { get; set; }

    /// <summary>
    /// Gets or sets the symbol of the stock, representing its unique identifier in the market.
    /// </summary>
    [Required]
    public required string Symbol { get; set; }
    
    /// <summary>
    /// Gets or sets the quantity of the stock held in the portfolio.
    /// </summary>
    [Required]
    public required decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the current total value of the stock in the portfolio.
    /// </summary>
    [Required]
    public required decimal CurrentTotalValue { get; set; }

    /// <summary>
    /// Gets or sets the total base value of the stock in the portfolio.
    /// </summary>
    [Required]
    public required decimal TotalBaseValue { get; set; }

    /// <summary>
    /// Gets or sets the percentage change of the stock value in the portfolio.
    /// </summary>
    [Required]
    public required decimal PercentageChange { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the portfolio stock was last updated.
    /// </summary>
    [Required]
    public required DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the portfolio associated with this stock.
    /// </summary>
    public required virtual Portfolio Portfolio { get; set; } = null!;
}
