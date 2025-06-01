namespace Invaise.BusinessDomain.API.Enums;
/// <summary>
/// Represents the status of an AI model.
/// </summary>
public enum AIModelStatus
{
    /// <summary>
    /// Indicates that the AI model is active and operational.
    /// </summary>
    Active,
    /// <summary>
    /// Indicates that the AI model is inactive and not operational.
    /// </summary>
    Inactive,
    /// <summary>
    /// Indicates that the AI model is currently in training.
    /// </summary>
    Training,
    /// <summary>
    /// Indicates that the AI model has failed and is not operational.
    /// </summary>
    Failed
}