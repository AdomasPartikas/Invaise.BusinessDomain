using Invaise.BusinessDomain.API.Entities;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Interface for gathering and managing predictions from AI models
/// </summary>
public interface IModelPredictionService
{

    Task RefreshAllPredictionsAsync();

    /// <summary>
    /// Gets the latest prediction for a symbol from a specific model source
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="modelSource">The source model (e.g., "Apollo", "Ignis")</param>
    /// <returns>The latest prediction if available</returns>
    Task<Prediction?> GetLatestPredictionAsync(string symbol, string modelSource);
    
    /// <summary>
    /// Gets all available predictions for a symbol from all model sources
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Dictionary mapping model sources to predictions</returns>
    Task<Dictionary<string, Prediction>> GetAllLatestPredictionsAsync(string symbol);
    
    /// <summary>
    /// Stores a new prediction from a model
    /// </summary>
    /// <param name="prediction">The prediction to store</param>
    /// <returns>The stored prediction with ID assigned</returns>
    Task<Prediction> StorePredictionAsync(Prediction prediction);
    
    /// <summary>
    /// Gets historical predictions for a symbol from a specific model
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="modelSource">The source model</param>
    /// <param name="startDate">The start date for historical data</param>
    /// <param name="endDate">The end date for historical data</param>
    /// <returns>List of historical predictions</returns>
    Task<IEnumerable<Prediction>> GetHistoricalPredictionsAsync(string symbol, string modelSource, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Refreshes predictions for a symbol from all model sources that are not Gaia
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <returns>Dictionary mapping model sources to refreshed predictions</returns>
    Task<Dictionary<string, Prediction>> RefreshPredictionsAsync(string symbol);

    /// <summary>
    /// Refreshes predictions for a portfolio from Gaia
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>Dictionary mapping model sources to refreshed predictions</returns>
    Task<Dictionary<string, Prediction>> RefreshPortfolioPredictionsAsync(string portfolioId);
} 