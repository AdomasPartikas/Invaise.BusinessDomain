using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a financial transaction within the system.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the unique identifier of the user associated with the transaction.
    /// </summary>
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the portfolio associated with the transaction.
    /// </summary>
    [ForeignKey("Portfolio")]
    public string PortfolioId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol associated with the transaction.
    /// </summary>
    [Required]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of shares involved in the transaction.
    /// </summary>
    [Required]
    public required decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price per share for the transaction.
    /// </summary>
    [Required]
    public required decimal PricePerShare { get; set; }

    /// <summary>
    /// Gets or sets the total value of the transaction.
    /// </summary>
    [Required]
    public decimal TransactionValue { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction occurred.
    /// </summary>
    [Required]
    public required DateTime TransactionDate { get; set; }

    /// <summary>
    /// Gets or sets the type of the transaction (e.g., Buy or Sell).
    /// </summary>
    [Required]
    public required TransactionType Type { get; set; } = TransactionType.Buy;

    /// <summary>
    /// Gets or sets the trigger that initiated the transaction.
    /// </summary>
    [Required]
    public required AvailableTransactionTriggers TriggeredBy { get; set; } = AvailableTransactionTriggers.System;

    /// <summary>
    /// Gets or sets the status of the transaction (e.g., OnHold, Completed, Failed).
    /// </summary>
    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.OnHold;

    /// <summary>
    /// Gets or sets the user associated with the transaction.
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the portfolio associated with the transaction.
    /// </summary>
    public virtual Portfolio Portfolio { get; set; } = null!;
}