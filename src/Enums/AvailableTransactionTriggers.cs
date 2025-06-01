namespace Invaise.BusinessDomain.API.Enums;
/// <summary>
/// Represents the available triggers for transactions.
/// </summary>
public enum AvailableTransactionTriggers
{
    /// <summary>
    /// Represents a system-triggered transaction.
    /// </summary>
    System,
    /// <summary>
    /// Represents a user-triggered transaction.
    /// </summary>
    User,
    /// <summary>
    /// Represents an AI-triggered transaction.
    /// </summary>
    AI,
    /// <summary>
    /// Represents a test-triggered transaction.
    /// </summary>
        Test
}
