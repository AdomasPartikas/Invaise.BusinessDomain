using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;
public class PortfolioStock
{
    [Key]
    public int ID { get; set; }

    [Required]
    public required int PortfolioId { get; set; }

    [Required]
    public required int CompanyId { get; set; }

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
    public required virtual Company Company { get; set; }
}
