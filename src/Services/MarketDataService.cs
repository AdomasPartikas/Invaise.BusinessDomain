using CsvHelper;
using CsvHelper.Configuration;
using AutoMapper;
using Invaise.BusinessDomain.API.FinnhubAPIClient;
using System.Globalization;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.API.Services;

public enum CallType
{
    MarketDataDaily,
    CompanyProfile
}

/// <summary>
/// Service for handling market data operations.
/// </summary>
public class MarketDataService(IFinnhubClient finnhubClient, IMapper mapper, InvaiseDbContext context, IKaggleService kaggleService, IDataService dataService, IDatabaseService dbService, Serilog.ILogger logger) : IMarketDataService
{
    public async Task FetchAndImportHistoricalMarketDataAsync()
    {
        await kaggleService.DownloadDatasetAsync(GlobalConstants.KaggleSmpDataset);

        await dataService.SMPDatasetCleanupAsync();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dataPath = Path.GetFullPath(Path.Combine(baseDirectory, GlobalConstants.DataFolder));

        var cleanedFilePath = Path.GetFullPath(Path.Combine(dataPath, GlobalConstants.SmpDataset));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            ShouldSkipRecord = args => args.Row.Parser.Record!.All(string.IsNullOrEmpty)
        };

        using var reader = new StreamReader(cleanedFilePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.TypeConverterOptionsCache.GetOptions<decimal?>().NullValues.Add(string.Empty);

        try
        {

            var newData = csv.GetRecords<MarketDataDto>().ToList();

            var grouped = newData.GroupBy(x => x.Symbol);

            var newEntities = new List<HistoricalMarketData>();

            foreach (var group in grouped)
            {
                var s = group.Key;

                // Get the latest date from DB for this symbol
                var latestDate = await context.HistoricalMarketData
                    .Where(m => m.Symbol == s)
                    .MaxAsync(m => (DateTime?)m.Date) ?? DateTime.MinValue;

                // Filter only newer entries
                var freshRecords = group.Where(g => g.Date > latestDate);

                foreach (var record in freshRecords)
                {
                    var entity = mapper.Map<HistoricalMarketData>(record);
                    newEntities.Add(entity);
                }
            }

            if (newEntities.Count > 0)
            {
                await context.HistoricalMarketData.AddRangeAsync(newEntities);
                await context.SaveChangesAsync();
                Console.WriteLine($"Inserted {newEntities.Count} new market data records.");
            }
            else
                Console.WriteLine("No new market data to insert.");
        }
        catch (CsvHelper.TypeConversion.TypeConverterException ex)
        {
            Console.WriteLine($"Error processing CSV: {ex.Message}");
        }
    }

    public async Task<bool> IsMarketOpenAsync()
    {
        try
        {
            var exchange = "US";
            var response = await finnhubClient.MarketStatusAsync(exchange);
            return response.IsOpen ?? false;
        }
        catch (FinnhubAPIClientException ex)
        {
            logger.Error(ex, "Error checking market status. Status: {Status}", ex.StatusCode);
            return false; // Default to market closed if we can't determine status
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error checking market status");
            return false; // Default to market closed if we can't determine status
        }
    }

    private async Task<object?> FinnhubCallWithRetries(string symbol, CallType callType, int retries = 5, int delay = 1)
    {
        int retryCount = 0;

        do
        {
            await Task.Delay(delay * 5);

            try
            {
                object? response = null;

                switch (callType)
                {
                    case CallType.MarketDataDaily:
                        response = await finnhubClient.QuoteAsync(symbol);
                        break;
                    case CallType.CompanyProfile:
                        response = await finnhubClient.CompanyProfile2Async(symbol, null, null);
                        break;
                }

                return response;
            }
            catch (FinnhubAPIClientException ex)
            {
                if (ex.StatusCode == 429)
                {
                    retryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching company data for {symbol}: {ex.Message}");
                return null;
            }

        } while (retryCount < retries);

        return null;
    }

    public async Task ImportIntradayMarketDataAsync()
    {

        if (!await IsMarketOpenAsync())
        {
            Console.WriteLine("Market is closed. Skipping import.");

            return;
        }

        var symbols = (await dbService.GetAllUniqueMarketDataSymbolsAsync())
            .OrderBy(symbol => symbol)
            .ToList();

        var retries = GlobalConstants.Retries;
        var delay = GlobalConstants.RetryDelaySeconds;

        foreach (var symbol in symbols)
        {
            var response = await FinnhubCallWithRetries(symbol, CallType.MarketDataDaily, retries, delay);


            if (response is Quote quote && quote != null)
            {
                var marketDataDaily = mapper.Map<IntradayMarketData>(quote);

                marketDataDaily.Symbol = symbol;

                var existingMarketDataDaily = await context.IntradayMarketData
                    .FirstOrDefaultAsync(m => m.Symbol == symbol && m.Timestamp == marketDataDaily.Timestamp);

                if (existingMarketDataDaily == null)
                    context.IntradayMarketData.Add(marketDataDaily);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ImportCompanyDataAsync()
    {
        var symbols = (await dbService.GetAllUniqueMarketDataSymbolsAsync())
            .OrderBy(symbol => symbol)
            .ToList();

        var retries = GlobalConstants.Retries;
        var delay = GlobalConstants.RetryDelaySeconds;

        foreach (var symbol in symbols)
        {
            var existingCompany = await context.Companies
                .FirstOrDefaultAsync(c => c.Symbol == symbol);

            if (existingCompany != null && existingCompany.Name != null && existingCompany.Description != null &&
                existingCompany.Country != null && existingCompany.Industry != null && existingCompany.Symbol != null)
            {
                continue;
            }
            else if (existingCompany == null)
            {
                existingCompany = new Entities.Company { Symbol = symbol };
                context.Companies.Add(existingCompany);
            }

            var company = await FinnhubCallWithRetries(symbol, CallType.CompanyProfile, retries, delay);

            if (company is CompanyProfile2 companyProfile && companyProfile != null)
            {
                mapper.Map(companyProfile, existingCompany);
            }
        }

        await context.SaveChangesAsync();
    }
}