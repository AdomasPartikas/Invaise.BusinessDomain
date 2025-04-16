using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;

public class Portfolio
{
    [Key]
    public int Id { get; set; }

    [Required]
    public long UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Default";

    [Required]
    public PortfolioStrategy StrategyDescription { get; set; } = PortfolioStrategy.Balanced;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUpdated { get; set; }


    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    public virtual ICollection<PortfolioStock> PortfolioStocks { get; set; } = new List<PortfolioStock>();

}