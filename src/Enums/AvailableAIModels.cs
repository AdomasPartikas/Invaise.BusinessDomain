namespace Invaise.BusinessDomain.API.Enums;
/// <summary>
/// Specifies the available AI models.
/// </summary>
public enum AvailableAIModels
{
    /// <summary>
    /// Represents the Apollo AI model.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "Apollo")]
    Apollo,

    /// <summary>
    /// Represents the Ignis AI model.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "Ignis")]
    Ignis,

    /// <summary>
    /// Represents the Gaia AI model.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "Gaia")]
    Gaia
}