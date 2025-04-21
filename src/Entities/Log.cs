using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Entities;


//Since we are using specific service for logging, we are adding this entity to the database.
//And the annotations are used to map the properties to the database columns.


public class Log
{
    [Key]
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message{ get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? CorrelationId { get; set; } = string.Empty;
    public string? ProcessId { get; set; } = string.Empty;
    public string? ProcessName { get; set; } = string.Empty;
    public string? ClientIp { get; set; } = string.Empty;
    public string Properties { get; set; } = string.Empty;
}