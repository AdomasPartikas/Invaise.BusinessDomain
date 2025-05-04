namespace Invaise.BusinessDomain.API.Enums;

/// <summary>
/// Represents the status of a transaction
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is on hold, waiting to be processed
    /// </summary>
    OnHold,
    
    /// <summary>
    /// Transaction has been successfully processed
    /// </summary>
    Succeeded,
    
    /// <summary>
    /// Transaction has been canceled
    /// </summary>
    Canceled,
    
    /// <summary>
    /// Transaction failed to process
    /// </summary>
    Failed
} 