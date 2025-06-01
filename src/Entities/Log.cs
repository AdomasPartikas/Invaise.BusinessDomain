using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;


//Since we are using specific service for logging, we are adding this entity to the database.
//And the annotations are used to map the properties to the database columns.


/// <summary>
/// Represents a log entry in the database, used for storing logging information.
/// </summary>
public class Log
{
    /// <summary>
    /// Gets or sets the unique identifier for the log entry.
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the timestamp of the log entry.
    /// </summary>
    public DateTime TimeStamp { get; set; }
    /// <summary>
    /// Gets or sets the level of the log entry (e.g., Info, Warning, Error).
    /// </summary>
    public string Level { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message associated with the log entry.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the template of the message associated with the log entry.
    /// </summary>
    public string MessageTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the exception details associated with the log entry, if any.
    /// </summary>
    public string? Exception { get; set; }
    /// <summary>
    /// Gets or sets the correlation identifier associated with the log entry.
    /// </summary>
    public string? CorrelationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the process identifier associated with the log entry.
    /// </summary>
    public string? ProcessId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the process associated with the log entry.
    /// </summary>
    public string? ProcessName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the client IP address associated with the log entry.
    /// </summary>
    public string? ClientIp { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the additional properties associated with the log entry.
    /// </summary>
    public string Properties { get; set; } = string.Empty;
}