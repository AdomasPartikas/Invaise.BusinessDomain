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
    public async Task<Heat?> GetHeatPredictionAsync(string symbol)
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
            double heatScore = 0;
            double confidence = 0.7; // Default confidence if not provided

            // Extract heat score from response (handle various types)
            switch (response.Heat_score)
            {
                case double doubleValue:
                    heatScore = doubleValue;
                    break;
                case long longValue:
                    heatScore = (double)longValue;
                    break;
                case int intValue:
                    heatScore = (double)intValue;
                    break;
                case string stringValue:
                    if (double.TryParse(stringValue, out var parsedValue))
                    {
                        heatScore = parsedValue;
                    }
                    else
                    {
                        // Try to parse it as a JSON string
                        try
                        {
                            var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(stringValue);
                            if (jsonObj != null && jsonObj.TryGetValue("heat_score", out var heatToken))
                            {
                                heatScore = heatToken.ToObject<double>();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Warning("Failed to parse heat_score from JSON string: {Error}", ex.Message);
                        }
                    }
                    break;
                case Newtonsoft.Json.Linq.JObject jObject:
                    // Try to get the heat score directly from the JObject
                    if (jObject.TryGetValue("heat_score", out var hsValue))
                    {
                        heatScore = hsValue.ToObject<double>();
                    }
                    else if (jObject.TryGetValue("value", out var jValue) && jValue.Type == Newtonsoft.Json.Linq.JTokenType.Float)
                    {
                        heatScore = jValue.ToObject<double>();
                    }
                    else
                    {
                        logger.Warning("Unexpected JObject format for Heat_score: {JObject}", jObject);
                        // For now, we'll set a default heat score of 0 for unknown formats
                        heatScore = 0;
                    }
                    break;
                case Newtonsoft.Json.Linq.JArray jArray:
                    // Handle array structure - if we see this array structure, default to 0
                    logger.Warning("Received JArray for Heat_score: {JArray}", jArray);
                    heatScore = 0;
                    break;
                default:
                    logger.Warning("Unexpected type for Heat_score: {Type}", response.Heat_score?.GetType());
                    break;
            }

            // Extract confidence if available
            if (response.AdditionalProperties.TryGetValue("confidence", out var confidenceObj) && 
                confidenceObj is double confidenceValue)
            {
                confidence = confidenceValue;
            }

            // Explanation might be in AdditionalProperties if not directly provided
            string explanation = response.AdditionalProperties.TryGetValue("explanation", out var explanationObj) && 
                               explanationObj is string explanationStr 
                               ? explanationStr 
                               : $"Apollo prediction for {symbol}: Heat score {heatScore:F2}";

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
            logger.Error(ex, "Error getting heat prediction from Apollo for symbol {Symbol}", symbol);
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
                logger.Error(ex, "Error getting heat prediction from Apollo for symbol {Symbol}", symbol);
            }
        }
        
        return result;
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