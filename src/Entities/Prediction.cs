using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a prediction made by an AI model
/// </summary>
public class Prediction
{
    /// <summary>
    /// Gets or sets the unique identifier for the prediction
    /// </summary>
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Gets or sets the stock symbol this prediction is for
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source of the model that made this prediction
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ModelSource { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the version of the model that made this prediction
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ModelVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when this prediction was made
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the target date for this prediction
    /// </summary>
    [Required]
    public DateTime PredictionTarget { get; set; }
    
    /// <summary>
    /// Gets or sets the current price at the time of prediction
    /// </summary>
    public decimal CurrentPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the predicted price
    /// </summary>
    public decimal PredictedPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the heat prediction associated with this prediction
    /// </summary>
    public Heat? Heat { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the associated heat prediction
    /// </summary>
    public long? HeatId { get; set; }
} 