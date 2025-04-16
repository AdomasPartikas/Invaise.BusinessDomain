using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;

public class Transaction
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
    public required decimal PricePerShare { get; set; }

    [Required]
    public decimal TransactionValue { get; set; }

    [Required]
    public required DateTime TransactionDate { get; set; }

    [Required]
    public required TransactionType Type { get; set; } = TransactionType.Buy;

    [Required]
    public required AvailableTransactionTriggers TriggeredBy { get; set; } = AvailableTransactionTriggers.System;


    public virtual Portfolio Portfolio { get; set; } = null!;
    public virtual Company Company { get; set; } = null!;
}