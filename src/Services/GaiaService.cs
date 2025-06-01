using AutoMapper;
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
        Serilog.ILogger logger,
        IMapper mapper) : IGaiaService
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
    public async Task<(Heat, DateTime, double)?> GetHeatPredictionAsync(string symbol, string portfolioId)
    {
        try
        {
            var request = new PredictionRequest 
            { 
                Symbol = symbol,
                Portfolio_id = portfolioId
            };
            
            var response = await gaiaPredictClient.PostAsync(request);
            
            if (response == null)
                return null;
            
            var heat = mapper.Map<Heat>(response);

            var prediction = response.Combined_heat.Predicted_price;
            var targetDate = DateTime.Parse(response.Combined_heat.Prediction_target, System.Globalization.CultureInfo.InvariantCulture);

            return (heat, targetDate, prediction);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting heat prediction from Gaia for {Symbol}", symbol);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PortfolioOptimizationResult> OptimizePortfolioAsync(string portfolioId)
    {
        try
        {
            var request = new OptimizationRequest
            {
                Portfolio_id = portfolioId
            };
            
            var response = await gaiaOptimizeClient.PostAsync(request);
            
            if (response == null)
            {
                return new PortfolioOptimizationResult
                {
                    UserId = "unknown",
                    Timestamp = DateTime.UtcNow.ToLocalTime(),
                    Explanation = "Failed to get optimization from Gaia"
                };
            }
            
            
            return mapper.Map<PortfolioOptimizationResult>(response);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error optimizing portfolio with Gaia for portfolio {PortfolioId}", portfolioId);
            
            return new PortfolioOptimizationResult
            {
                UserId = "unknown",
                Timestamp = DateTime.UtcNow.ToLocalTime(),
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
} 