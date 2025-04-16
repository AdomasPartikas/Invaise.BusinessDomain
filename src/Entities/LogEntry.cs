using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;


public class LogEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    public AvailableServices Service { get; set; } = AvailableServices.BusinessDomainAPI;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public LogLevel Level { get; set; } = LogLevel.Information;

    public long? UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}