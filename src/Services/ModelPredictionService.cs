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

    public async Task<Dictionary<string, Prediction>> RefreshPortfolioPredictionsAsync(string portfolioId)
    {
        var result = new Dictionary<string, Prediction>();

        try
        {
            // Get all symbols in the portfolio
            var portfolio = await databaseService.GetPortfolioByIdWithPortfolioStocksAsync(portfolioId);
            if (portfolio == null) return result;
            
            var symbols = portfolio.PortfolioStocks.Select(s => s.Symbol).ToList();
            
            foreach (var symbol in symbols)
            {
                var prediction = await FetchAndStorePredictionFromGaia(symbol, portfolioId);
                if (prediction != null)
                {
                    result[symbol] = prediction;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error refreshing portfolio predictions for {PortfolioId}", portfolioId);
            return result;
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

    #region Private Helper Methods

    private async Task<Prediction?> FetchAndStorePredictionFromApollo(string symbol)
    {
        try
        {
            // Get heat prediction from Apollo service
            var response = await apolloService.GetHeatPredictionAsync(symbol);

            if (response is null) return null;
            
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
                PredictionTarget = DateTime.UtcNow.Date.AddDays(30), // Apollo predicts for next month
                CurrentPrice = currentPrice,
                PredictedPrice = (decimal)response.Value.Item2, // Simple example
                Heat = response.Value.Item1
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
            var response = await ignisService.GetHeatPredictionAsync(symbol);
            
            if (response is null) return null;
            
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
                PredictedPrice = (decimal)response.Value.Item2, // Simple example
                Heat = response.Value.Item1
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
    
    private async Task<Prediction?> FetchAndStorePredictionFromGaia(string symbol, string portfolioId)
    {
        try
        {
            // Get heat prediction from Gaia service
            var heat = await gaiaService.GetHeatPredictionAsync(symbol, portfolioId);
            
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
                PredictionTarget = heat.Value.Item2, // Gaia combines Apollo and Ignis, we'll use a shorter timeframe
                CurrentPrice = currentPrice,
                PredictedPrice = (decimal)heat.Value.Item3, // Simple example based on heat score
                Heat = heat.Value.Item1
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
    
    #endregion
} 