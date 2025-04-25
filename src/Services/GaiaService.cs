using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.GaiaAPIClient;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.Extensions.Logging;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for interacting with the Gaia AI model
/// </summary>
public class GaiaService(
        IHealthGaiaClient gaiaHealthClient, 
        IPredictGaiaClient gaiaPredictClient,
        IOptimizeGaiaClient gaiaOptimizeClient,
        IWeightsGaiaClient gaiaWeightsClient,
        Serilog.ILogger logger) : IGaiaService
{

    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await gaiaHealthClient.GetAsync();
            
            if (response == null)
            {
                return false;
            }
            
            return response.Status == "ok";
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error checking Gaia health status");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetModelVersionAsync()
    {
        try
        {
            var response = await gaiaHealthClient.GetAsync();
            
            return response?.Version ?? "unknown";
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting Gaia model version");
            return "unknown";
        }
    }

    /// <inheritdoc />
    public async Task<Heat?> GetHeatPredictionAsync(string symbol, string? userId = null)
    {
        try
        {
            var request = new PredictionRequest 
            { 
                Symbol = symbol
            };

            if (!string.IsNullOrEmpty(userId))
            {
                request.User_id = userId != null 
                    ? new User_id { AdditionalProperties = new Dictionary<string, object> { { "user_id", userId } } }
                    : new User_id { AdditionalProperties = new Dictionary<string, object>() };
            }
            
            var response = await gaiaPredictClient.PostAsync(request);
            
            if (response == null)
                return null;
            
            return MapToHeat(response.Combined_heat, symbol);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting heat prediction from Gaia for {Symbol}", symbol);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Heat>> GetHeatPredictionsAsync(IEnumerable<string> symbols, string? userId = null)
    {
        var result = new Dictionary<string, Heat>();
        
        try
        {
            // Gaia doesn't have a batch prediction endpoint, so we need to call it sequentially
            foreach (var symbol in symbols)
            {
                var heat = await GetHeatPredictionAsync(symbol, userId);
                if (heat != null)
                {
                    result[symbol] = heat;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting heat predictions from Gaia");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string userId, IEnumerable<string> symbols)
    {
        try
        {
            var request = new OptimizationRequest
            {
                User_id = userId,
                Symbols = symbols.ToList()
            };
            
            var response = await gaiaOptimizeClient.PostAsync(request);
            
            if (response == null)
            {
                return new PortfolioOptimizationResult
                {
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Explanation = "Failed to get optimization from Gaia"
                };
            }
            
            // Convert the dynamic response to a typed response
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var gaiaResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GaiaOptimizationResponse>(json);
            
            return MapToOptimizationResult(gaiaResponse);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error optimizing portfolio with Gaia for user {UserId}", userId);
            
            return new PortfolioOptimizationResult
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Explanation = $"Error optimizing portfolio: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> AdjustModelWeightsAsync(double apolloWeight, double ignisWeight)
    {
        try
        {
            var request = new WeightAdjustRequest
            {
                Apollo_weight = apolloWeight,
                Ignis_weight = ignisWeight
            };
            
            await gaiaWeightsClient.PostAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error adjusting model weights in Gaia");
            return false;
        }
    }

    #region Private Helper Methods

    private Heat MapToHeat(HeatData heatData, string symbol)
    {
        return new Heat
        {
            Symbol = symbol,
            HeatScore = heatData.Heat_score,
            Score = (int)(heatData.Heat_score * 100), // Normalize to 0-100 scale
            Confidence = (int)(heatData.Confidence * 100), // Normalize to 0-100 scale
            Explanation = heatData.Explanation,
            ApolloContribution = heatData.Apollo_contribution,
            IgnisContribution = heatData.Ignis_contribution
        };
    }

    private PortfolioOptimizationResult MapToOptimizationResult(GaiaOptimizationResponse response)
    {
        var result = new PortfolioOptimizationResult
        {
            UserId = response.UserId,
            Explanation = response.Explanation,
            Confidence = response.Confidence,
            Timestamp = DateTime.Parse(response.Timestamp)
        };
        
        foreach (var recommendation in response.PortfolioRecommendations)
        {
            result.Recommendations.Add(new PortfolioRecommendation
            {
                Symbol = recommendation.Symbol,
                Action = recommendation.Action,
                CurrentQuantity = recommendation.CurrentQuantity,
                TargetQuantity = recommendation.TargetQuantity,
                CurrentWeight = recommendation.CurrentWeight,
                TargetWeight = recommendation.TargetWeight,
                Explanation = recommendation.Explanation
            });
        }
        
        return result;
    }

    #endregion

    #region Response Models
    
    private class GaiaOptimizationResponse
    {
        public string UserId { get; set; } = string.Empty;
        public List<GaiaRecommendation> PortfolioRecommendations { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }

    private class GaiaRecommendation
    {
        public string Symbol { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal TargetQuantity { get; set; }
        public double CurrentWeight { get; set; }
        public double TargetWeight { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
    
    #endregion
} 