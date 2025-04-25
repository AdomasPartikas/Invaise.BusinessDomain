using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invaise.BusinessDomain.API.Entities;

/// <summary>
/// Represents a heat prediction for a stock
/// </summary>
public class Heat
{
    /// <summary>
    /// Gets or sets the unique identifier for the heat prediction
    /// </summary>
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Gets or sets the stock symbol this heat prediction is for
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the heat score (0-100)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int Score { get; set; }
    
    /// <summary>
    /// Gets or sets the confidence level (0-100)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int Confidence { get; set; }

    /// <summary>
    /// Gets or sets the heat score from the model
    /// </summary>
    public double HeatScore { get; set; }

    /// <summary>
    /// Gets or sets the explanation for the heat prediction
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Gets or sets the direction of the heat prediction (e.g., "up", "down", "neutral")
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contribution from Apollo model
    /// </summary>
    public double? ApolloContribution { get; set; }

    /// <summary>
    /// Gets or sets the contribution from Ignis model
    /// </summary>
    public double? IgnisContribution { get; set; }
    
    /// <summary>
    /// Gets or sets the prediction that this heat belongs to
    /// </summary>
    [ForeignKey("PredictionId")]
    public Prediction? Prediction { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the associated prediction
    /// </summary>
    public long PredictionId { get; set; }
} 