using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;

public class Portfolio
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Default";

    [Required]
    public PortfolioStrategy StrategyDescription { get; set; } = PortfolioStrategy.Balanced;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the portfolio is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<PortfolioStock> PortfolioStocks { get; set; } = new List<PortfolioStock>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}