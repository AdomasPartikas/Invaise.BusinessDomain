using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;
public class PortfolioStock
{
    [Key]
    public string ID { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [ForeignKey("Portfolio")]
    public required string PortfolioId { get; set; }

    [Required]
    public required string Symbol { get; set; }
    
    [Required]
    public required decimal Quantity { get; set; }

    [Required]
    public required decimal CurrentTotalValue { get; set; }

    [Required]
    public required decimal TotalBaseValue { get; set; }

    [Required]
    public required decimal PercentageChange { get; set; }

    [Required]
    public required DateTime LastUpdated { get; set; }


    public required virtual Portfolio Portfolio { get; set; } = null!;
}
