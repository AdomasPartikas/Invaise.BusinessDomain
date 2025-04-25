using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Constants;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for gathering and managing predictions from AI models
/// </summary>
public class ModelPredictionService(
        IDatabaseService databaseService,
        InvaiseDbContext dbContext,
        IApolloService apolloService,
        IIgnisService ignisService,
        IGaiaService gaiaService,
        IMarketDataService marketDataService,
        Serilog.ILogger logger) : IModelPredictionService
{
    // Using constants from ModelConstants
    private const string APOLLO_SOURCE = ModelConstants.APOLLO_SOURCE;
    private const string IGNIS_SOURCE = ModelConstants.IGNIS_SOURCE;
    private const string GAIA_SOURCE = ModelConstants.GAIA_SOURCE;

    /// <inheritdoc />
    public async Task<Prediction?> GetLatestPredictionAsync(string symbol, string modelSource)
    {
        try
        {
            return await dbContext.Predictions
                .Include(p => p.Heat)
                .Where(p => p.Symbol == symbol && p.ModelSource == modelSource)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error retrieving latest prediction for {Symbol} from {ModelSource}", symbol, modelSource);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Prediction>> GetLatestPredictionsAsync(IEnumerable<string> symbols, string modelSource)
    {
        var result = new Dictionary<string, Prediction>();
        
        try
        {
            var symbolsList = symbols.ToList();
            
            // Get the most recent prediction for each symbol from the specified model source
            var latestPredictions = await dbContext.Predictions
                .Include(p => p.Heat)
                .Where(p => symbolsList.Contains(p.Symbol) && p.ModelSource == modelSource)
                .GroupBy(p => p.Symbol)
                .Select(g => g.OrderByDescending(p => p.Timestamp).FirstOrDefault())
                .ToListAsync();
                
            foreach (var prediction in latestPredictions.Where(p => p != null))
            {
                result[prediction.Symbol] = prediction;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error retrieving latest predictions from {ModelSource}", modelSource);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Prediction>> GetAllLatestPredictionsAsync(string symbol)
    {
        var result = new Dictionary<string, Prediction>();
        
        try
        {
            // Get the most recent prediction for the symbol from each model source
            var latestPredictions = await dbContext.Predictions
                .Include(p => p.Heat)
                .Where(p => p.Symbol == symbol)
                .GroupBy(p => p.ModelSource)
                .Select(g => g.OrderByDescending(p => p.Timestamp).FirstOrDefault())
                .ToListAsync();
                
            foreach (var prediction in latestPredictions.Where(p => p != null))
            {
                result[prediction.ModelSource] = prediction;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error retrieving all latest predictions for {Symbol}", symbol);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<Prediction> StorePredictionAsync(Prediction prediction)
    {
        try
        {
            // Ensure we have a timestamp
            if (prediction.Timestamp == default)
            {
                prediction.Timestamp = DateTime.UtcNow;
            }
            
            dbContext.Predictions.Add(prediction);
            await dbContext.SaveChangesAsync();
            return prediction;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error storing prediction for {Symbol} from {ModelSource}", 
                prediction.Symbol, prediction.ModelSource);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Prediction>> GetHistoricalPredictionsAsync(
        string symbol, string modelSource, DateTime startDate, DateTime endDate)
    {
        try
        {
            return await dbContext.Predictions
                .Include(p => p.Heat)
                .Where(p => p.Symbol == symbol && 
                           p.ModelSource == modelSource &&
                           p.Timestamp >= startDate &&
                           p.Timestamp <= endDate)
                .OrderBy(p => p.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error retrieving historical predictions for {Symbol} from {ModelSource}", 
                symbol, modelSource);
            return Enumerable.Empty<Prediction>();
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Prediction>> RefreshPredictionsAsync(string symbol)
    {
        var result = new Dictionary<string, Prediction>();
        
        try
        {
            // Get prediction from Apollo (daily predictor)
            var apolloPrediction = await FetchAndStorePredictionFromApollo(symbol);
            if (apolloPrediction != null)
            {
                result[APOLLO_SOURCE] = apolloPrediction;
            }
            
            // Get prediction from Ignis (real-time predictor)
            var ignisPrediction = await FetchAndStorePredictionFromIgnis(symbol);
            if (ignisPrediction != null)
            {
                result[IGNIS_SOURCE] = ignisPrediction;
            }
            
            // Get prediction from Gaia (ensemble predictor)
            var gaiaPrediction = await FetchAndStorePredictionFromGaia(symbol);
            if (gaiaPrediction != null)
            {
                result[GAIA_SOURCE] = gaiaPrediction;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error refreshing predictions for {Symbol}", symbol);
            return result;
        }
    }

    public async Task RefreshAllPredictionsAsync()
    {
        try
        {
            // Get all symbols from the database
            var companies = await databaseService.GetAllCompaniesAsync();
            var symbols = companies.Select(c => c.Symbol).ToList();
            
            // Refresh predictions for each symbol
            foreach (var symbol in symbols)
            {
                await RefreshPredictionsAsync(symbol);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error refreshing all predictions");
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Dictionary<string, Prediction>>> RefreshPredictionsAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, Dictionary<string, Prediction>>();
        var symbolsList = symbols.ToList();
        
        try
        {
            // Get predictions from Apollo (daily predictor) for all symbols
            var apolloPredictions = await FetchAndStorePredictionsFromApollo(symbolsList);
            
            // Get predictions from Ignis (real-time predictor) for all symbols
            var ignisPredictions = await FetchAndStorePredictionsFromIgnis(symbolsList);
            
            // Get predictions from Gaia (ensemble predictor) for all symbols
            var gaiaPredictions = await FetchAndStorePredictionsFromGaia(symbolsList);
            
            // Organize results by symbol
            foreach (var symbol in symbolsList)
            {
                var symbolPredictions = new Dictionary<string, Prediction>();
                
                if (apolloPredictions.TryGetValue(symbol, out var apolloPrediction))
                {
                    symbolPredictions[APOLLO_SOURCE] = apolloPrediction;
                }
                
                if (ignisPredictions.TryGetValue(symbol, out var ignisPrediction))
                {
                    symbolPredictions[IGNIS_SOURCE] = ignisPrediction;
                }
                
                if (gaiaPredictions.TryGetValue(symbol, out var gaiaPrediction))
                {
                    symbolPredictions[GAIA_SOURCE] = gaiaPrediction;
                }
                
                if (symbolPredictions.Count > 0)
                {
                    result[symbol] = symbolPredictions;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error refreshing predictions for multiple symbols");
            return result;
        }
    }

    #region Private Helper Methods

    private async Task<Prediction?> FetchAndStorePredictionFromApollo(string symbol)
    {
        try
        {
            // Get heat prediction from Apollo service
            var heat = await apolloService.GetHeatPredictionAsync(symbol);

            if (heat is null) return null;
            
            // Get current price for the symbol from market data service
            var historicalMarketDataLatest = await databaseService.GetLatestHistoricalMarketDataAsync(symbol);
            var currentPrice = historicalMarketDataLatest?.Close ?? 0;
            
            // Create and store prediction
            var prediction = new Prediction
            {
                Symbol = symbol,
                ModelSource = APOLLO_SOURCE,
                Timestamp = DateTime.UtcNow,
                ModelVersion = await apolloService.GetModelVersionAsync(),
                PredictionTarget = DateTime.UtcNow.Date.AddDays(1), // Apollo predicts for next day
                CurrentPrice = currentPrice,
                PredictedPrice = (heat?.HeatScore ?? 0) > 0.5 ? currentPrice * 1.01m : currentPrice * 0.99m, // Simple example
                Heat = heat
            };
            
            await StorePredictionAsync(prediction);
            return prediction;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching prediction from Apollo for {Symbol}", symbol);
            return null;
        }
    }
    
    private async Task<Prediction?> FetchAndStorePredictionFromIgnis(string symbol)
    {
        try
        {
            // Get heat prediction from Ignis service
            var heat = await ignisService.GetHeatPredictionAsync(symbol);
            
            if (heat is null) return null;
            
            // Get current price for the symbol from market data service
            var intradayMarketDataLatest = await databaseService.GetLatestIntradayMarketDataAsync(symbol);
            var currentPrice = intradayMarketDataLatest?.Current ?? 0;
            
            // Create and store prediction
            var prediction = new Prediction
            {
                Symbol = symbol,
                ModelSource = IGNIS_SOURCE,
                Timestamp = DateTime.UtcNow,
                ModelVersion = await ignisService.GetModelVersionAsync(),
                PredictionTarget = DateTime.UtcNow.Date.AddMinutes(IgnisConstants.IgnisPredictionPeriod), // Ignis is real-time, so target is current time
                CurrentPrice = currentPrice,
                PredictedPrice = (heat?.HeatScore ?? 0) > 0.5 ? currentPrice * 1.01m : currentPrice * 0.99m, // Simple example
                Heat = heat
            };
            
            await StorePredictionAsync(prediction);
            return prediction;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching prediction from Ignis for {Symbol}", symbol);
            return null;
        }
    }
    
    private async Task<Prediction?> FetchAndStorePredictionFromGaia(string symbol)
    {
        try
        {
            // Get heat prediction from Gaia service
            var heat = await gaiaService.GetHeatPredictionAsync(symbol);
            
            if (heat is null) return null;
            
            // Get current price for the symbol from market data service
            var intradayMarketDataLatest = await databaseService.GetLatestIntradayMarketDataAsync(symbol);
            var currentPrice = intradayMarketDataLatest?.Current ?? 0;
            
            // Create and store prediction
            var prediction = new Prediction
            {
                Symbol = symbol,
                ModelSource = GAIA_SOURCE,
                Timestamp = DateTime.UtcNow,
                ModelVersion = await gaiaService.GetModelVersionAsync(),
                PredictionTarget = DateTime.UtcNow.Date.AddHours(1), // Gaia combines Apollo and Ignis, we'll use a shorter timeframe
                CurrentPrice = currentPrice,
                PredictedPrice = (heat?.HeatScore ?? 0) > 0.5 ? currentPrice * 1.01m : currentPrice * 0.99m, // Simple example based on heat score
                Heat = heat
            };
            
            await StorePredictionAsync(prediction);
            return prediction;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching prediction from Gaia for {Symbol}", symbol);
            return null;
        }
    }
    
    private async Task<Dictionary<string, Prediction>> FetchAndStorePredictionsFromApollo(List<string> symbols)
    {
        try
        {
            // Get heat predictions from Apollo service for all symbols
            var heats = await apolloService.GetHeatPredictionsAsync(symbols);
            var predictions = new Dictionary<string, Prediction>();
            
            if (heats == null || !heats.Any()) return predictions;
            
            var modelVersion = await apolloService.GetModelVersionAsync();
            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            
            // Create predictions for all symbols with heat data
            foreach (var (symbol, heat) in heats)
            {
                // Get current price for the symbol from market data service
                var historicalMarketDataLatest = await databaseService.GetLatestHistoricalMarketDataAsync(symbol);
                var currentPrice = historicalMarketDataLatest?.Close ?? 0;
                
                var prediction = new Prediction
                {
                    Symbol = symbol,
                    ModelSource = APOLLO_SOURCE,
                    Timestamp = DateTime.UtcNow,
                    ModelVersion = modelVersion,
                    PredictionTarget = tomorrow, // Apollo predicts for next day
                    CurrentPrice = currentPrice,
                    PredictedPrice = (decimal?)(heat?.HeatScore ?? 0) > 0.5m ? currentPrice * 1.01m : currentPrice * 0.99m, // Simple example
                    Heat = heat
                };
                
                await StorePredictionAsync(prediction);
                predictions[symbol] = prediction;
            }
            
            return predictions;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching predictions from Apollo for multiple symbols");
            return new Dictionary<string, Prediction>();
        }
    }
    
    private async Task<Dictionary<string, Prediction>> FetchAndStorePredictionsFromIgnis(List<string> symbols)
    {
        try
        {
            // Get heat predictions from Ignis service for all symbols
            var heats = await ignisService.GetHeatPredictionsAsync(symbols);
            var predictions = new Dictionary<string, Prediction>();
            
            if (heats == null || !heats.Any()) return predictions;
            
            var modelVersion = await ignisService.GetModelVersionAsync();
            var now = DateTime.UtcNow;
            
            // Create predictions for all symbols with heat data
            foreach (var (symbol, heat) in heats)
            {
                // Get current price for the symbol from market data service
                var intradayMarketDataLatest = await databaseService.GetLatestIntradayMarketDataAsync(symbol);
                var currentPrice = intradayMarketDataLatest?.Current ?? 0;
                
                var prediction = new Prediction
                {
                    Symbol = symbol,
                    ModelSource = IGNIS_SOURCE,
                    Timestamp = now,
                    ModelVersion = modelVersion,
                    PredictionTarget = now, // Ignis is real-time
                    CurrentPrice = currentPrice,
                    PredictedPrice = (decimal?)(heat?.HeatScore ?? 0) > 0.5m ? currentPrice * 1.01m : currentPrice * 0.99m, // Simple example
                    Heat = heat
                };
                
                await StorePredictionAsync(prediction);
                predictions[symbol] = prediction;
            }
            
            return predictions;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching predictions from Ignis for multiple symbols");
            return new Dictionary<string, Prediction>();
        }
    }
    
    private async Task<Dictionary<string, Prediction>> FetchAndStorePredictionsFromGaia(List<string> symbols)
    {
        try
        {
            // Get heat predictions from Gaia service for all symbols
            var heats = await gaiaService.GetHeatPredictionsAsync(symbols);
            var predictions = new Dictionary<string, Prediction>();
            
            if (heats == null || !heats.Any()) return predictions;
            
            var modelVersion = await gaiaService.GetModelVersionAsync();
            var now = DateTime.UtcNow;
            
            // Create predictions for all symbols with heat data
            foreach (var (symbol, heat) in heats)
            {
                // Get current price for the symbol from market data service
                var intradayMarketDataLatest = await databaseService.GetLatestIntradayMarketDataAsync(symbol);
                var currentPrice = intradayMarketDataLatest?.Current ?? 0;
                
                var prediction = new Prediction
                {
                    Symbol = symbol,
                    ModelSource = GAIA_SOURCE,
                    Timestamp = now,
                    ModelVersion = modelVersion,
                    PredictionTarget = now.AddHours(1), // Gaia combines Apollo and Ignis
                    CurrentPrice = currentPrice,
                    PredictedPrice = (decimal?)(heat?.HeatScore ?? 0) > 0.5m ? currentPrice * 1.01m : currentPrice * 0.99m, // Simple example
                    Heat = heat
                };
                
                await StorePredictionAsync(prediction);
                predictions[symbol] = prediction;
            }
            
            return predictions;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching predictions from Gaia for multiple symbols");
            return new Dictionary<string, Prediction>();
        }
    }
    
    #endregion
} 