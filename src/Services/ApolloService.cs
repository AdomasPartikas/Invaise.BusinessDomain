using Invaise.BusinessDomain.API.ApolloAPIClient;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.Extensions.Logging;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for interacting with the Apollo AI model
/// </summary>
public class ApolloService(
    IHealthApolloClient apolloHealthClient, 
    IPredictApolloClient apolloPredictClient, 
    IInfoApolloClient apolloInfoClient,
    ITrainApolloClient apolloTrainClient,
    IStatusApolloClient apolloStatusClient,
    Serilog.ILogger logger) : IApolloService
{

    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await apolloHealthClient.GetAsync();

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
            var response = await apolloPredictClient.GetAsync(symbol, ApolloConstants.ApolloPredictionPeriod, ApolloConstants.ApolloLookbackPeriod);
            
            if (response == null)
            {
                return null;
            }

            // The Heat_score in the response is an object type
            // We need to safely convert it to a double
            double heatScore = 0;  // Default confidence if not provided

            // Extract heat score from response (handle various types)
            heatScore = response.Heat_score;
            // Extract confidence if available

            var confidence = response.Confidence;

            var direction = response.Direction ?? "neutral"; // Default to neutral if not provided

            // Explanation might be in AdditionalProperties if not directly provided
            string explanation = response.Explanation ?? "No explanation provided";

            var prediction = response.Predicted_next_close;

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
            logger.Error(ex, "Error getting heat prediction from Apollo for symbol {Symbol}", symbol);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RequestRetrainingAsync()
    {
        try
        {
            var response = await apolloTrainClient.PostAsync();
            return response.Success;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error requesting retraining from Apollo");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsTrainingAsync()
    {
        try
        {
            var response = await apolloStatusClient.GetAsync();
            
            if (response == null)
            {
                logger.Warning("Null response received from Apollo status endpoint");
                return false;
            }
            
            return response.Is_training;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting Apollo status");
            
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
            var response = await apolloInfoClient.GetAsync();
            return response.Model_version ?? "unknown";
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting Apollo model version");
            return "unknown";
        }
    }
} 