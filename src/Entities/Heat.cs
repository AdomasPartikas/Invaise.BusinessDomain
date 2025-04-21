using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Models;

public class Heat
{
    [Key]
    public int Id { get; set; }
    
    [ForeignKey(nameof(Prediction))]
    public int PredictionId { get; set; }
    
    [Required]
    [Range(0, 1)]
    public double HeatScore { get; set; }
    
    [Required]
    [Range(0, 1)]
    public double Confidence { get; set; }
    
    [Required]
    public string Explanation { get; set; } = string.Empty;
    
    // Effective signal strength
    [Range(0, 1)]
    public double EffectiveHeat => HeatScore * Confidence;
    
    // For ensemble models like Gaia
    public double? ApolloContribution { get; set; }
    public double? IgnisContribution { get; set; }
    
    // Navigation property
    public virtual Prediction Prediction { get; set; } = null!;
}