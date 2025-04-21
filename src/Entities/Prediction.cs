using System.ComponentModel.DataAnnotations;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Models;

public class Prediction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    public string ModelSource { get; set; } = string.Empty;
    
    [Required]
    public string ModelVersion { get; set; } = string.Empty;
    
    // Navigation property
    public virtual Heat Heat { get; set; } = null!;
    
    // Additional prediction details
    public double? PredictedValue { get; set; }
    public DateTime? PredictionTarget { get; set; }  // For what time this prediction is made
    
    // Technical indicators or other data that led to this prediction
    public string InputFeatures { get; set; } = string.Empty;
}