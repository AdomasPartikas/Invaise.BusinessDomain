using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a portfolio entity containing user-specific investment details.
/// </summary>
public class Portfolio
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the unique identifier for the user associated with the portfolio.
    /// </summary>
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the portfolio.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the strategy description for the portfolio.
    /// </summary>
    [Required]
    public PortfolioStrategy StrategyDescription { get; set; } = PortfolioStrategy.Balanced;

    /// <summary>
    /// Gets or sets the date and time when the portfolio was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the date and time when the portfolio was last updated.
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the portfolio is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Gets or sets the user associated with the portfolio.
    /// </summary>
    public virtual User User { get; set; } = null!;
    /// <summary>
    /// Gets or sets the collection of stocks associated with the portfolio.
    /// </summary>
    public virtual ICollection<PortfolioStock> PortfolioStocks { get; set; } = new List<PortfolioStock>();
    /// <summary>
    /// Gets or sets the collection of transactions associated with the portfolio.
    /// </summary>
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}