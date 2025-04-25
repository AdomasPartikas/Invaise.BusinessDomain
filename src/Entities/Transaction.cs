using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

public class Transaction
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("Portfolio")]
    public string PortfolioId { get; set; } = string.Empty;

    [Required]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public required decimal Quantity { get; set; }

    [Required]
    public required decimal PricePerShare { get; set; }

    [Required]
    public decimal TransactionValue { get; set; }

    [Required]
    public required DateTime TransactionDate { get; set; }

    [Required]
    public required TransactionType Type { get; set; } = TransactionType.Buy;

    [Required]
    public required AvailableTransactionTriggers TriggeredBy { get; set; } = AvailableTransactionTriggers.System;

    public virtual User User { get; set; } = null!;
    public virtual Portfolio Portfolio { get; set; } = null!;
}