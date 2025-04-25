using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;

public class PortfolioHealth
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string PortfolioId { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public decimal TotalValue { get; set; }

    [Required]
    public decimal TotalProfitLoss { get; set; }

    public decimal? Volatility { get; set; }

    public decimal? SharpeRatio { get; set; }

    public decimal? DiversificationScore { get; set; }

    public decimal? MaxDrawdown { get; set; }

    public decimal RiskAdjustedReturn { get; set; }

    public virtual Portfolio Portfolio { get; set; } = null!;
}