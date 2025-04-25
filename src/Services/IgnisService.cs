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
    public async Task<Heat?> GetHeatPredictionAsync(string symbol)
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
            double confidence = 0.7; // Default confidence

            heatScore = response.Heat_score;

            // Extract confidence if available
            if (response.AdditionalProperties.TryGetValue("confidence", out var confidenceObj) && 
                confidenceObj is double confidenceValue)
            {
                confidence = confidenceValue;
            }

            // Get explanation or generate a default one
            string explanation = response.AdditionalProperties.TryGetValue("explanation", out var explanationObj) && 
                               explanationObj is string explanationStr 
                               ? explanationStr 
                               : $"Ignis prediction for {symbol}: Heat score {heatScore:F2}";

            var heat = new Heat
            {
                Symbol = symbol,
                HeatScore = heatScore,
                Score = (int)Math.Round(heatScore * 100),
                Confidence = (int)Math.Round(confidence * 100),
                Explanation = explanation
            };

            return heat;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting heat prediction from Ignis for symbol {Symbol}", symbol);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, Heat>> GetHeatPredictionsAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, Heat>();
        
        foreach (var symbol in symbols)
        {
            try
            {
                var prediction = await GetHeatPredictionAsync(symbol);
                if (prediction != null)
                {
                    result.Add(symbol, prediction);
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other symbols
                logger.Error(ex, "Error getting heat prediction from Ignis for symbol {Symbol}", symbol);
            }
        }
        
        return result;
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