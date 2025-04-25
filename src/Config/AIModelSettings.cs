namespace Invaise.BusinessDomain.API.Config;

/// <summary>
/// Configuration settings for AI models
/// </summary>
public class AIModelSettings
{
    /// <summary>
    /// Gets or sets the Apollo API settings
    /// </summary>
    public AIModelApiSettings Apollo { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the Ignis API settings
    /// </summary>
    public AIModelApiSettings Ignis { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the Gaia API settings
    /// </summary>
    public AIModelApiSettings Gaia { get; set; } = new();
}

/// <summary>
/// Configuration settings for an AI model API
/// </summary>
public class AIModelApiSettings
{
    /// <summary>
    /// Gets or sets the base URL for the API
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the API key (if required)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
} 