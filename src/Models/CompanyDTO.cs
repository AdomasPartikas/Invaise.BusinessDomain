using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a company entity with details such as stock information, name, industry, and creation date.
/// </summary>
public class CompanyDto
{
    /// <summary>
    /// Gets or sets the stock symbol of the company.
    /// This field is required and has a maximum length of 10 characters.
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the company.
    /// This field is required and has a maximum length of 255 characters.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the industry of the company.
    /// This field is optional and has a maximum length of 255 characters.
    /// </summary>
    [MaxLength(255)]
    public string? Industry { get; set; }

    /// <summary>
    /// Gets or sets the description of the company.
    /// This field is optional and has a maximum length of 1000 characters.
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country where the company is located.
    /// This field is optional and has a maximum length of 255 characters.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the company entity was created.
    /// Defaults to the current UTC date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
}