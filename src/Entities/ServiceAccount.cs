using System.ComponentModel.DataAnnotations;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a service account entity with properties such as Id, Name, Key, Role, Permissions, Created date, and LastAuthenticated date.
/// </summary>
public class ServiceAccount
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the service account.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key for the service account.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user (e.g., Admin, User).
    /// </summary>
    [Required]
    public string Role { get; set; } = "Service";

    /// <summary>
    /// Gets or sets the permissions associated with the service account.
    /// </summary>
    public string[] Permissions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the created date and time of the service account.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the last auth date and time of the service account.
    /// </summary>
    public DateTime? LastAuthenticated { get; set; }
}