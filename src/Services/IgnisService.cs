using Invaise.BusinessDomain.API.IgnisAPIClient;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Constants;
using Microsoft.Extensions.Logging;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for interacting with the Ignis AI model
/// </summary>
public class IgnisService(
    IHealthIgnisClient ignisHealthClient, 
    IPredictIgnisClient ignisPredictClient, 
    IInfoIgnisClient ignisInfoClient,
    ITrainIgnisClient ignisTrainClient,
    IStatusIgnisClient ignisStatusClient,
    Serilog.ILogger logger) : IIgnisService
{
    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await ignisHealthClient.GetAsync();
            
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

    /// <inheritdoc/>
    public async Task<(Heat, double)?> GetHeatPredictionAsync(string symbol)
    {
        try
        {
            var response = await ignisPredictClient.GetAsync(symbol, null);
            
            if (response == null)
            {
                return null;
            }

            // Extract heat score and confidence from response
            double heatScore = 0;

            heatScore = response.Heat_score;

            double confidence = response.Confidence;

            // Get explanation or generate a default one
            string explanation = response.Explanation ?? "No explanation available";

            string direction = response.Direction ?? "neutral";

            var prediction = response.Pred_close;

            var heat = new Heat
            {
                Symbol = symbol,
                HeatScore = heatScore,
                Score = (int)Math.Round(heatScore * 100),
                Confidence = (int)Math.Round(confidence * 100),
                Explanation = explanation,
                Direction = direction,
            };

            return (heat, prediction);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting heat prediction from Ignis for symbol {Symbol}", symbol);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RequestRetrainingAsync()
    {
        try
        {
            var response = await ignisTrainClient.PostAsync();
            return response.Success;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error requesting retraining from Ignis");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsTrainingAsync()
    {
        try
        {
            var response = await ignisStatusClient.GetAsync();
            
            if (response == null)
            {
                logger.Warning("Null response received from Ignis status endpoint");
                return false;
            }
            
            return response.Status == TrainingStatus.Training;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting Ignis status");
            
            // Return false to prevent cascading errors
            // In case of error, we assume the model is not training
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetModelVersionAsync()
    {
        try
        {
            var response = await ignisInfoClient.GetAsync();
            return response.Model_version ?? "unknown";
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting Ignis model version");
            return "unknown";
        }
    }
} 